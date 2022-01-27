# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.3.0] - 2022-01-27

### Added

-  Add support for `AutoConstructorInject` on property backing field

### Issues

- [#14](https://github.com/k94ll13nn3/AutoConstructor/issues/14): Support also get-only properties

## [2.2.0] - 2022-01-22

### Added

- Add support for nested classes

### Issues

- [#13](https://github.com/k94ll13nn3/AutoConstructor/issues/13): Support Nested Classes

## [2.1.0] - 2022-01-21

### Added

- Add support for generating constructor comment

## [2.0.2] - 2021-11-11

### Changed

- [**Breaking**] Move to incremental generator. Visual Studio 17.0+ and .NET SDK 6.0.100+ are now required

### Pull Requests

- [#9](https://github.com/k94ll13nn3/AutoConstructor/pull/9): Move to incremental generator (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [1.3.0] - 2021-09-21

### Added

- Add support for generating nullable code when needed

### Changed

- Change parameters of `AutoConstructorInjectAttribute` to be optionals
- Change ACONS05 to be reported on each attribute

### Fixed

- Fix usage of attributes with aliases
- Fix ACONS01 detection

## [1.2.1] - 2021-08-28

### Fixed

- Fix ACONS06 to be reported on the concerned file

## [1.2.0] - 2021-08-28

### Added

- Add ACONS06 diagnostic

### Fixed

- Fix existing diagnostics doc

## [1.1.0] - 2021-08-26

### Added

- Add `AutoConstructor_DisableNullChecking` configuration

## [1.0.2] - 2021-08-02

### Fixed

- Fix generated attributes visibility

## [1.0.1] - 2021-07-24

### Fixed

- Fix PackageReference generation as analyzer

## 1.0.0 - 2021-07-23

Initial release

[2.3.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v2.2.0...v2.3.0
[2.2.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v2.0.2...v2.1.0
[2.0.2]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.3.0...v2.0.2
[1.3.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.2.1...v1.3.0
[1.2.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v1.0.0...v1.0.1
