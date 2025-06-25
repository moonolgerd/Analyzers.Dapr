---
name: Feature Request
about: Suggest a new analyzer rule or improvement
title: '[FEATURE] '
labels: ['enhancement']
assignees: ['moonolgerd']
---

## Feature Description
A clear and concise description of the new analyzer rule or feature you'd like to see.

## Problem/Use Case
Describe the problem this feature would solve or the use case it addresses:
- What Dapr Actor pattern or best practice should be enforced?
- What common mistakes should be caught?
- How does this relate to Dapr documentation or guidelines?

## Proposed Rule
If suggesting a new analyzer rule:
- **Rule ID**: [e.g., DAPR011]
- **Severity**: [Error/Warning/Info/Hidden]
- **Category**: [e.g., Serialization, Interface, Performance]

## Code Examples

### Bad (should trigger diagnostic):
```csharp
// Code that should be flagged by the analyzer
public class BadExample : Actor
{
    // Your example here
}
```

### Good (should not trigger diagnostic):
```csharp
// Code that follows best practices
[DataContract]
public class GoodExample : Actor
{
    // Your example here
}
```

## Expected Diagnostic Message
What message should the analyzer show to developers?

## Code Fix
Should this rule include an automatic code fix? If so, describe what the fix should do.

## Additional Context
- Link to relevant Dapr documentation
- Examples from other projects
- Any other context that would help implement this feature
