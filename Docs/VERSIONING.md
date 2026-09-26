# Versioning

The project uses Semantic Versioning where practical:

- **MAJOR:** incompatible pack/runtime architecture or public API change
- **MINOR:** a major translator capability or substantial compatible feature
- **PATCH:** fixes, translation corrections, compatible QA/tool improvements

Private development may remain ahead of public releases. Promotion states are:

1. `DEV`
2. `PUBLIC-CANDIDATE`
3. `PUBLIC-READY`
4. `RELEASED`

The mod assembly version, language-pack version, public candidate version, changelog, and release manifest must agree before publication. A Git tag is created only for an intentionally released public state, never automatically by promotion tooling.
