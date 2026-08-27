# Development progress

This document tracks user-facing changes while they are being developed. It is the source material for NuGet package release notes, GitHub releases, and relevant README updates.

## Unreleased — 0.0.6

### Highlights

- Search multiple directories in one combine operation.
- Filter discovered solution files by name with case-insensitive regular expressions.
- Use an explicit `combine` command while retaining the tool's combine workflow as the default command.

### Added

- Multiple values are accepted for the optional `TraverseDirectory` argument. Files discovered through overlapping paths are de-duplicated before they are combined.
- `--include <REGEX>` includes only `.sln` and `.slnx` files whose filename, without its extension, matches the expression.
- `--exclude <REGEX>` removes matching `.sln` and `.slnx` files from discovery. It can be combined with `--include` for narrower selection.
- Include and exclude expressions are matched case-insensitively.
- Invalid regular expressions are reported as command validation errors before file discovery starts.
- The combine workflow is also registered as the `combine` command.
- Tests cover multiple traversal directories, duplicate discovery, include/exclude behavior, case-insensitive matching, and invalid expressions.

### Changed

- The command implementation was renamed from `RunCommand` to `CombineCommand` to describe its purpose more clearly.
- The package version was advanced from `0.0.5` to `0.0.6`.

### Release preparation

- [x] Update the NuGet package release notes.
- [x] Change the README usage signature to show that multiple traversal directories are supported.
- [x] Document the `--include`, `--exclude`, and `--overwrite` options.
- [x] Add examples for combining multiple source directories and filtering solution names.
- [x] Document both direct invocation and the explicit `combine` command form.
- [x] Update the installation example so it does not pin the outdated `0.0.2` version.
