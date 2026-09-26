# Audio

Audio replacement is optional and experimental. `Audio/manifest.json` maps observed clip identities to local replacement files. The runtime cache and coroutine preload path exist, but the maintained pt-BR manifest is empty and no public playback pack has completed acceptance testing.

Do not claim audio localization support from an empty manifest. A future acceptance pass must verify file decoding, `AudioClip` wrapper compatibility, repeated playback, scene changes, cleanup, latency, and volume/loop behavior using assets the contributor may distribute.

Original game audio, extracted banks, and game-owned clips are excluded from source and public candidates.
