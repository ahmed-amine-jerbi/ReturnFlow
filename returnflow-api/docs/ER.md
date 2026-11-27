# Data model

```mermaid
erDiagram
    AppUser ||--o{ ReturnRequest : "creates"
    ReturnRequest ||--o{ ReturnClaim : "has"
    ReturnClaim ||--o{ Decision : "decided by (one or more)"
    AppUser ||--o{ Decision : "makes (agents only)"
    AppUser ||--o{ AuditLog : "performs"

    AppUser {
        int Id PK
        string Email UK
        string PasswordHash
        string Role
        datetime CreatedAt
    }
    ReturnRequest {
        int Id PK
        int CustomerId FK
        string OrderReference
        date PurchaseDate
        string Reason
        string Status
        datetime CreatedAt
    }
    ReturnClaim {
        int Id PK
        int ReturnRequestId FK
        string Description
        decimal AmountRequested
        string Status
        string Processing
        string RoutingReason
        bool HumanReviewRequested
        datetime ReviewRequestedAt
        string AiCategory
        double AiRiskScore
        double AiConfidence
        text_array AiReasons
        string AiModelVersion
        datetime AnalyzedAt
        datetime CreatedAt
        xid xmin "row version, concurrency token"
    }
    Decision {
        int Id PK
        int ClaimId FK
        int AgentId FK "null when automatic"
        string DecidedBy "Auto or Human"
        string Outcome
        string Comment
        datetime DecidedAt
    }
    AuditLog {
        long Id PK
        int UserId "null for system actions"
        string Action
        string EntityType
        int EntityId
        datetime Timestamp
    }
```

## Claim workflow (`Processing`)

`Analyzing` -> `AutoDecided` or `NeedsHuman`
`AutoDecided` -> `HumanReviewRequested` (customer or staff asks) -> `HumanDecided`
`NeedsHuman` -> `HumanDecided`

## Business rules

- A customer can only see and extend their own returns.
- A claim starts `Open` and can be decided only while open. A human review reopens an automatically decided claim and keeps the earlier decision in the history.
- Only `Agent`, `Manager` and `Admin` can decide claims; only `Manager` and `Admin` can read statistics.
- The AI recommends; `DecisionPolicy` decides whether a claim is handled automatically.
