# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [5.3.0] - 2024-03-16

### Changed

- Add XML documentation to generated attributes
- Replace `AutoConstructor_DisableNullChecking` with `AutoConstructor_GenerateArgumentNullException`
- Add option to configure `base` calls behavior
- Add option to configure `this` calls behavior

### Issues

- [#109](https://github.com/k94ll13nn3/AutoConstructor/issues/109): Replace AutoConstructor_DisableNullChecking with AutoConstructor_GenerateArgumentNullException
- [#108](https://github.com/k94ll13nn3/AutoConstructor/issues/108): XML doc tags
- [#107](https://github.com/k94ll13nn3/AutoConstructor/issues/107): Better control of Base / this constructors

### Pull Requests

- [#111](https://github.com/k94ll13nn3/AutoConstructor/pull/111): Fix warnings and update CI actions (by [k94ll13nn3](https://github.com/k94ll13nn3))
- [#110](https://github.com/k94ll13nn3/AutoConstructor/pull/110): Add configurations for preferences on `this` and `base` calls (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [5.2.1] - 2024-03-04

### Fixed

-  Fix detection of base constructor if parent class defined in another assembly and is using `AutoConstructor`

### Issues

- [#106](https://github.com/k94ll13nn3/AutoConstructor/issues/106): Upgrading from version 4.1.1 to 5.2.0 broke AutoConstructor on subtypes

## [5.2.0] - 2023-12-19

### Changed

- Add a diagnostic suppression on `CS0436` (diagnostic reported when using `InternalsVisibleTo`)

### Issues

- [#93](https://github.com/k94ll13nn3/AutoConstructor/issues/93): Use across assemblies that use `[assembly: InternalsVisibleTo(...)]` produces warnings

## [5.1.0] - 2023-11-23

### Changed

- Add support for struct types and different type kind nesting (by @Sergio0694)

### Pull Requests

- [#89](https://github.com/k94ll13nn3/AutoConstructor/pull/89): Add support for struct types and different type kind nesting (by [Sergio0694](https://github.com/Sergio0694))

## [5.0.1] - 2023-11-22

### Fixed

- Fix call to static initializer method
- Fix generation when a reserved keyword is used (directly or indirectly)
- Fix edge cases on MismatchTypesRule diagnostic

## [5.0.0] - 2023-11-07

### Added

- Add new argument to `AutoConstructorAttribute` for specifiying constructor accessibility
- Add `AutoConstructorInitializer` used to add a call to a method at the end of the constructor

### Changed

- [**Breaking**] Update Roslyn dependencies. Visual Studio 17.6+ and .NET SDK 7.0.302+ are now required
- Rework code generation

### Fixed

- Fix incrementability of generator

### Issues

- [#82](https://github.com/k94ll13nn3/AutoConstructor/issues/82): The source generator is completely non incremental
- [#21](https://github.com/k94ll13nn3/AutoConstructor/issues/21): Allow injection of method call inside the constructor

### Pull Requests

- [#88](https://github.com/k94ll13nn3/AutoConstructor/pull/88): Rework code generation with indented writer (by [k94ll13nn3](https://github.com/k94ll13nn3))
- [#83](https://github.com/k94ll13nn3/AutoConstructor/pull/83): Update generator to really be incremental (by [k94ll13nn3](https://github.com/k94ll13nn3))
- [#54](https://github.com/k94ll13nn3/AutoConstructor/pull/54): Add support for constructor access modifier (by [k94ll13nn3](https://github.com/k94ll13nn3))
- [#27](https://github.com/k94ll13nn3/AutoConstructor/pull/27): Allow injection of method call inside the constructor (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [4.1.1] - 2023-07-18

### Fixed

- Fix wrong call to `this` when a static constructor is found

### Issues

- [#71](https://github.com/k94ll13nn3/AutoConstructor/issues/71): Erroneous call to default ctor generated if the type declares a static constructor

### Pull Requests

- [#72](https://github.com/k94ll13nn3/AutoConstructor/pull/72): Fix call to this with static constructor (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [4.1.0] - 2023-07-08

### Changed

- Add call to parameterless constructor if one is defined 

### Issues

- [#70](https://github.com/k94ll13nn3/AutoConstructor/issues/70): Call default/empty constructor if exists

## [4.0.3] - 2023-04-24

### Fixed

- Fix calls to base constructor if the class also has a static constructor
- Fix handling of AutoConstructor_DisableNullChecking when no value is provided

### Issues

- [#60](https://github.com/k94ll13nn3/AutoConstructor/issues/60): Fails to call base constructor if base has static constructor

### Pull Requests

- [#61](https://github.com/k94ll13nn3/AutoConstructor/pull/61): Fix static ctor in base class should be ignored (by [DomasM](https://github.com/DomasM))

## [4.0.2] - 2023-04-19

### Fixed

- Fix possible ArgumentException

## [4.0.1] - 2023-03-29

### Fixed

- Fix nullable context not generated when using base class

### Issues

- [#58](https://github.com/k94ll13nn3/AutoConstructor/issues/58): #nullable enable line not generated if only fields in base type are nullable

### Pull Requests

- [#59](https://github.com/k94ll13nn3/AutoConstructor/pull/59): fix/nullability-of-fields-not-extracted (by [DomasM](https://github.com/DomasM))

## [4.0.0] - 2023-03-27

### Changed

- [**Breaking**] Non get-only properties are now injected when using `AutoConstructorInject` (by @DomasM)
- Null checks are generated even in nullable context when enabled (by @DomasM)

### Pull Requests

- [#56](https://github.com/k94ll13nn3/AutoConstructor/pull/56): Check for nulls even in nullable enabled context (by [DomasM](https://github.com/DomasM))
- [#55](https://github.com/k94ll13nn3/AutoConstructor/pull/55): Allow explicit include of non-readonly properties in ctor (by [DomasM](https://github.com/DomasM))

## [3.2.5] - 2022-10-15

### Fixed

- Fix computation of nullability when using nullable types as generic parameters

## [3.2.4] - 2022-10-07

### Fixed

- Fix hintname for generic classes

## [3.2.3] - 2022-06-28

### Fixed

- Fix stack overflow in ACONS02 check

## [3.2.2] - 2022-06-27

### Fixed

- Fix ACONS02 false positive on classes with fields in parent

## [3.2.1] - 2022-06-11

### Fixed

- Fix generation of constructor for classes with parent class that is also generated

## [3.2.0] - 2022-05-20

### Added

- Add support for automatic constructor on classes with a parent class

### Issues

- [#20](https://github.com/k94ll13nn3/AutoConstructor/issues/20): Add support for automatic constructor on classes with a parent class

### Pull Requests

- [#31](https://github.com/k94ll13nn3/AutoConstructor/pull/31): Add initial support for generating constructor based on parent class (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [3.1.1] - 2022-05-14

### Added

- Add support for generating constructor for generic classes

### Issues

- [#29](https://github.com/k94ll13nn3/AutoConstructor/issues/29): Support generic classes

### Pull Requests

- [#30](https://github.com/k94ll13nn3/AutoConstructor/pull/30): Fix generation of constructor for generic classes (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [3.0.0] - 2022-04-01

### Changed

- [**Breaking**] Get-only properties are now injected by default
- [**Breaking**] Null checks are disabled by default
- Reworked code generation
- Update attributes header

### Fixed

- Fix generation for partial classes with multiple parts
- Fix generation of ArgumentNullException in nullable context

### Issues

- [#23](https://github.com/k94ll13nn3/AutoConstructor/issues/23): Disable null checks by default
- [#14](https://github.com/k94ll13nn3/AutoConstructor/issues/14): Support also get-only properties

### Pull Requests

- [#18](https://github.com/k94ll13nn3/AutoConstructor/pull/18): Rework code generation with Roslyn API (by [k94ll13nn3](https://github.com/k94ll13nn3))

## [2.3.0] - 2022-01-27

### Added

- Add support for `AutoConstructorInject` on property backing field

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

- Change parameters of `AutoConstructorInjectAttribute` to be optional
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

[5.3.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v5.2.1...v5.3.0
[5.2.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v5.2.0...v5.2.1
[5.2.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v5.1.0...v5.2.0
[5.1.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v5.0.1...v5.1.0
[5.0.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v5.0.0...v5.0.1
[5.0.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.1.1...v5.0.0
[4.1.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.1.0...v4.1.1
[4.1.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.0.3...v4.1.0
[4.0.3]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.0.2...v4.0.3
[4.0.2]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.0.1...v4.0.2
[4.0.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v4.0.0...v4.0.1
[4.0.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.5...v4.0.0
[3.2.5]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.4...v3.2.5
[3.2.4]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.3...v3.2.4
[3.2.3]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.2...v3.2.3
[3.2.2]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.1...v3.2.2
[3.2.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.2.0...v3.2.1
[3.2.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.1.1...v3.2.0
[3.1.1]: https://github.com/k94ll13nn3/AutoConstructor/compare/v3.0.0...v3.1.1
[3.0.0]: https://github.com/k94ll13nn3/AutoConstructor/compare/v2.3.0...v3.0.0
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
