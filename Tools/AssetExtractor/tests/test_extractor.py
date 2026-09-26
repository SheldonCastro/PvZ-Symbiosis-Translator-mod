import csv
import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from types import SimpleNamespace as NS

from PIL import Image

from pvz_asset_extractor import storage as s
from pvz_asset_extractor.cli import export_object
from pvz_asset_extractor.unity import discover, metadata


class ExtractorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def test_filename_invalid_characters(self):
        self.assertEqual(s.safe_name('a<>:"/\\|?*\x01z. '), 'a__________z')

    def test_reserved_names(self):
        for name in ('CON', 'nul.png', 'lpt1', 'COM9', 'AUX.txt', 'COM¹'):
            self.assertTrue(s.safe_name(name).startswith('_'))

    def test_empty_names(self):
        for name in ('', None, ' . ', '..'):
            self.assertEqual(s.safe_name(name, 'Texture2D'), 'Texture2D')

    def test_pathid_uniqueness(self):
        self.assertNotEqual(s.image_path('Sprite', 'a', 1, 'same'), s.image_path('Sprite', 'a', 2, 'same'))

    def test_source_collision(self):
        self.assertNotEqual(s.image_path('Sprite', 'a/b', 1, 'x'), s.image_path('Sprite', 'a_b', 1, 'x'))

    def test_unicode_and_length(self):
        self.assertEqual(s.safe_name('植物'), '植物')
        self.assertLessEqual(len(s.safe_name('x'*300)), 80)

    def test_manifest_serialization(self):
        path = self.root/'manifest.json'
        value = [{'pathId': 9223372036854775807, 'name': '植物', 'rect': {'x': 0.5}}]
        s.atomic_json(path, value)
        self.assertEqual(json.loads(path.read_text(encoding='utf-8')), value)

    def test_csv(self):
        path = self.root/'assets.csv'
        s.write_csv(path, [{'name': 'a,"b\nc', 'pathId': 1}, {'name': '=1+1'}])
        with path.open(encoding='utf-8-sig', newline='') as f:
            rows = list(csv.DictReader(f))
        self.assertEqual(rows[0]['name'], 'a,"b\nc')
        self.assertEqual(rows[1]['name'], "'=1+1")

    def test_hash(self):
        path = self.root/'file'
        path.write_bytes(b'abc')
        self.assertEqual(s.file_hash(path), hashlib.sha256(b'abc').hexdigest())

    def png_entry(self):
        path = self.root/'1_test.png'
        with Image.new('RGBA', (2, 3), (30, 50, 70, 90)) as image:
            image.save(path)
        return {'sourceFile': 'a.assets', 'pathId': 1, 'type': 'Texture2D', 'name': 'test',
                'status': 'exported', 'width': 2, 'height': 3, 'sha256': s.file_hash(path), 'exportedFile': path.name}

    def test_resume_valid(self):
        self.assertTrue(s.reusable(self.root, self.png_entry()))

    def test_resume_missing(self):
        entry = self.png_entry()
        (self.root/entry['exportedFile']).unlink()
        self.assertFalse(s.reusable(self.root, entry))

    def test_resume_corrupt_stops(self):
        entry = self.png_entry()
        (self.root/entry['exportedFile']).write_bytes(b'corrupt')
        with self.assertRaises(s.WorkspaceError):
            s.reusable(self.root, entry)

    def test_png_dimensions(self):
        entry = self.png_entry()
        entry['width'] = 4
        with self.assertRaises(ValueError):
            s.validate_png(self.root/entry['exportedFile'], entry)

    def test_duplicate_keeps_objects(self):
        entries = [{'pathId': 1, 'sha256': 'same'}, {'pathId': 2, 'sha256': 'same'}]
        s.mark_duplicates(entries)
        self.assertEqual(len(entries), 2)
        self.assertFalse(entries[0]['duplicateContent'])
        self.assertTrue(entries[1]['duplicateContent'])

    def test_invalid_input(self):
        with self.assertRaises(FileNotFoundError):
            s.input_directory(self.root/'missing')
        path = self.root/'file'; path.touch()
        with self.assertRaises(ValueError):
            s.input_directory(path)

    def test_overlap_prohibited(self):
        for path in (self.root, self.root/'output', self.root.parent):
            with self.assertRaises(ValueError):
                s.output_directory(self.root, path)

    def test_traversal(self):
        for name in ('../bad', '/bad', 'C:/bad', 'a/../../bad', 'a\\..\\bad', 'a:stream'):
            with self.assertRaises(s.WorkspaceError):
                s.contained(self.root, name)

    def test_state_roundtrip(self):
        state = s.State(self.root, {'files': {'a': 'hash'}})
        entry = {'sourceFile': 'a', 'pathId': 1, 'type': 'Sprite', 'status': 'failed'}
        state.record(entry)
        self.assertEqual(s.State(self.root, {'files': {'a': 'hash'}}).entries[s.asset_key(entry)], entry)

    def test_state_changed_input(self):
        s.State(self.root, {'files': {'a': 'hash'}})
        with self.assertRaises(ValueError):
            s.State(self.root, {'files': {'a': 'changed'}})

    def test_state_unknown_output(self):
        (self.root/'existing.png').touch()
        with self.assertRaises(ValueError):
            s.State(self.root, {})

    def test_state_interrupted_append(self):
        state = s.State(self.root, {})
        entry = {'sourceFile': 'a', 'pathId': 1, 'type': 'Sprite', 'status': 'failed'}
        state.record(entry)
        with state.journal.open('ab') as f:
            f.write(b'{"incomplete":')
        resumed = s.State(self.root, {})
        self.assertEqual(len(resumed.entries), 1)
        resumed.record(dict(entry, pathId=2))
        self.assertEqual(len(s.State(self.root, {}).entries), 2)

    def test_state_corrupt_complete_line(self):
        state = s.State(self.root, {})
        state.journal.write_bytes(b'bad\n')
        with self.assertRaises(json.JSONDecodeError):
            s.State(self.root, {})

    def test_snapshot_detects_resource_change(self):
        path = self.root/'a.resS'; path.write_bytes(b'one')
        before = s.snapshot(self.root)
        path.write_bytes(b'two')
        self.assertNotEqual(before, s.snapshot(self.root))

    def test_discovery_headers_and_streams(self):
        (self.root/'x.resS').write_bytes(b'UnityFS\0')
        (self.root/'bundle_without_extension').write_bytes(b'UnityFS\0')
        (self.root/'broken.assets').write_bytes(b'bad')
        (self.root/'ordinary.txt').write_text('not assets')
        (self.root/'serialized').write_bytes(struct.pack('>4I', 10, 128, 21, 64) + bytes(112))
        self.assertEqual({p.name for p in discover(self.root)}, {'bundle_without_extension', 'broken.assets', 'serialized'})

    def test_failure_isolated(self):
        class Broken:
            path_id = 1
            type = NS(name='Texture2D')
            def read(self):
                raise NotImplementedError('unsupported format')
        state = s.State(self.root, {})
        entry, reused = export_object(Broken(), 'a', self.root, state, False)
        self.assertFalse(reused)
        self.assertEqual(entry['status'], 'failed')
        self.assertEqual(entry['errorCategory'], 'NotImplementedError')

    def test_export_resume_does_not_decode(self):
        class Texture:
            path_id = 42
            type = NS(name='Texture2D')
            def read(self):
                return NS(m_Name='test', m_Width=2, m_Height=3, m_TextureFormat=4,
                          image=Image.new('RGBA', (2, 3)))
        obj = Texture(); state = s.State(self.root, {})
        entry, reused = export_object(obj, 'a', self.root, state, False)
        self.assertEqual(entry['status'], 'exported')
        state.record(entry)
        obj.read = lambda: self.fail('Decoded completed asset during resume')
        self.assertTrue(export_object(obj, 'a', self.root, state, False)[1])

    def test_sprite_metadata_omits_unavailable(self):
        obj = NS(path_id=5, type=NS(name='Sprite'))
        data = NS(m_Name='sprite', m_RD=NS(), m_Rect=NS(x=1,y=2,width=3,height=4))
        entry = metadata(obj, data, 'a')
        self.assertEqual(entry['rect']['width'], 3)
        self.assertNotIn('pivot', entry)
        self.assertNotIn('texturePathId', entry)


if __name__ == '__main__':
    unittest.main()
