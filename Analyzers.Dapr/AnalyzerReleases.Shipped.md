## Release 2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DAPR1405 | Usage | Error | Actor interface should inherit from IActor
DAPR1406 | Usage | Warning | Enum members in Actor types should use EnumMember attribute
DAPR1407 | Usage | Info | Consider using JsonPropertyName for property name consistency
DAPR1408 | Usage | Warning | Complex types used in Actor methods need serialization attributes
DAPR1409 | Usage | Warning | Actor method parameter needs proper serialization attributes
DAPR1410 | Usage | Warning | Actor method return type needs proper serialization attributes
DAPR1411 | Usage | Warning | Collection types in Actor methods need element type validation
DAPR1412 | Usage | Warning | Record types should use DataContract and DataMember attributes for Actor serialization
DAPR1413 | Usage | Error | Actor class implementation should implement an interface that inherits from IActor
DAPR1414 | Usage | Error | All types must either expose a public parameterless constructor or be decorated with the DataContractAttribute attribute

## Release 1.1

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DAPR008 | Serialization | Warning | Scope narrowed: only fires when record is used in a public IActor interface method
DAPR010 | Serialization | Error | Scope narrowed: only fires when type is used in a public IActor interface method

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DAPR001 | Interface | Error | Actor interface should inherit from IActor
DAPR002 | Serialization | Warning | Enum members in Actor types should use EnumMember attribute
DAPR003 | Serialization | Info | Consider using JsonPropertyName for property name consistency
DAPR004 | Serialization | Warning | Complex types used in Actor methods need serialization attributes
DAPR005 | Serialization | Warning | Actor method parameter needs proper serialization attributes
DAPR006 | Serialization | Warning | Actor method return type needs proper serialization attributes
DAPR007 | Serialization | Warning | Collection types in Actor methods need element type validation
DAPR008 | Serialization | Warning | Record types should use DataContract and DataMember attributes for Actor serialization
DAPR009 | Serialization | Error | Actor class implementation should implement an interface that inherits from IActor
DAPR010 | Serialization | Error | All types must either expose a public parameterless constructor or be decorated with the DataContractAttribute attribute
