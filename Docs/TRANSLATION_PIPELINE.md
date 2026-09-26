# Translation Pipeline

For each observed supported text component, the mod retains the original source and resolves in this order:

1. an exact mapping for the current scene/hierarchy context;
2. a global exact mapping;
3. an enabled dynamic rule matching the whole source;
4. the unchanged original.

Previously rendered translated targets are recognized so they are not recaptured as new source. Translation remains reversible because the source is retained per component.

Dynamic rules require `^...$`, named capture groups, a 20 ms regex timeout, and a target that references the same groups. They are limited to genuinely variable whole strings such as seconds or multipliers. Exact mappings intentionally override dynamic rules.

Rich-text policy derives support from the target and restores the component's original setting when translation is disabled. TMP and legacy Unity UI Text have different capabilities; unsupported legacy `TextMesh` enumeration is intentionally excluded for this game build.

Diagnostics and Translator Mode collect observed source/context records. Export files are authoring aids and do not become active translations until reviewed into the maintained locale.
