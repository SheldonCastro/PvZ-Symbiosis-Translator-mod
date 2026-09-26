# Quality Assurance

The QA service validates the active locale without mutating runtime translation state.

Checks cover JSON structure and duplicate keys, paths, exact markers, placeholders, markup, dynamic rules, source/target contamination, CJK residue, whitespace, encoding, suspicious length, textures, and audio.

Texture QA decodes PNG pixels rather than checking only the signature. It reports unsafe/missing replacements, unsupported files, duplicate IDs, duplicate explicit source identities, duplicate automatic filename-and-dimension identities, and explicit shadowing. Dimensions and source availability are compared only when the required runtime/catalog evidence exists; QA does not invent a source match.

Severity:

- **Error:** technically unsafe or unusable candidate
- **Warning:** review or compatibility risk
- **Info:** intentional/observable condition
- **Pass:** no findings for the check

Run from Settings > QA or through the test/build tools. Reports are written as JSON and readable text. A QA pass does not establish complete translation coverage or editorial approval.
