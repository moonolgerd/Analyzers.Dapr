# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-02-19

### Changed
- **DAPR008**: Narrowed scope — now only raised when a record type is used as a parameter or return type in a public IActor interface method. Standalone records that are not part of an IActor contract are no longer flagged.
- **DAPR010**: Narrowed scope — now only raised for types used in methods that implement an IActor interface. Public helper methods on the Actor class that are not part of the IActor contract are no longer flagged.

### Added
- GitHub Actions CI/CD workflows
- Automated dependency updates with Dependabot
- Security scanning with CodeQL
- Performance monitoring
- Documentation generation and validation
- Issue and pull request templates

## [1.0.0] - 2025-06-25

### Added
- Initial release with 10 diagnostic rules for Dapr Actor validation
- DAPR001: Actor interface should inherit from IActor
- DAPR002: Enum members should use EnumMember attribute
- DAPR003: Consider using JsonPropertyName for property name consistency
- DAPR004: Complex types used in Actor methods need serialization attributes
- DAPR005: Actor method parameter needs proper serialization attributes
- DAPR006: Actor method return type needs proper serialization attributes
- DAPR007: Collection types in Actor methods need element type validation
- DAPR008: Record types should use DataContract and DataMember attributes
- DAPR009: Actor class should implement an interface that inherits from IActor
- DAPR010: Types must have parameterless constructor or DataContract attribute
- Code fixes for most diagnostic rules
- Comprehensive test suite
- NuGet package configuration
- Documentation and examples

[Unreleased]: https://github.com/moonolgerd/Analyzers.Dapr/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/moonolgerd/Analyzers.Dapr/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/moonolgerd/Analyzers.Dapr/releases/tag/v1.0.0
