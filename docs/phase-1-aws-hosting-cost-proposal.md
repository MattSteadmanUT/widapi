# AWS Hosting and Cost Proposal (Phases 1 to 3)

**Date:** 2026-04-07  
**Context:** Planning assumptions for the private implementation repo. This public repo is for documentation and design history.

---

## Scope and Assumptions

This document estimates recurring AWS infrastructure cost only.

Included:

- API runtime and scheduling
- Aurora database
- authentication and authorization services
- storage, logs, and basic operational tooling

Not included:

- labor and project staffing
- premium support plans
- third-party tools
- legal/compliance effort

Current technical assumptions:

- Region: AWS GovCloud (US-West)
- Database: Aurora PostgreSQL Serverless v2
- Initial pricing mode: Aurora Standard
- API entry: API Gateway HTTP API
- API compute: Lambda first
- Batch compute: Lambda first, Fargate when job length or memory profile requires it
- Auth: Cognito user pool with role/group claims (including WID DBA)

GovCloud pricing note:

- AWS pricing pages do not always present GovCloud service rates in the same static examples used for commercial regions.
- This document uses the existing commercial baseline and applies a **20% GovCloud planning uplift** across the ranges.
- Use AWS Pricing Calculator with GovCloud (US-West) selected before final budget approval.

Pricing references used:

- Aurora Serverless example ACU pricing in us-east-1: $0.12 per ACU-hour (Aurora Standard)
- Aurora Standard example storage and I/O: $0.10 per GB-month and $0.20 per 1M I/O requests
- API Gateway HTTP API example rate: about $1.00 per 1M requests in lower tiers
- Lambda request pricing: $0.20 per 1M requests
- Cognito free-tier note: AWS documentation states Cognito free-tier pricing is not available in GovCloud (US)

---

## Phase 1 Baseline

Phase 1 is modeled as staff-only access for WID administration support, with scheduled data refresh and no public traffic.

Additional scope assumptions for this phase:

- no EmpDB
- no licensing-related tables in this phase
- statewide data where relevant

### Phase 1 monthly ranges (GovCloud-adjusted)

#### Scenario A: Lean pilot

- under 150 authenticated users
- 1 to 2 million API requests per month
- 50 GB Aurora storage
- average Aurora utilization around 0.75 ACU

| Component | Monthly estimate |
| --- | ---: |
| Aurora Serverless compute | $78 - $102 |
| Aurora storage and I/O | $10 - $24 |
| Lambda | $0 - $12 |
| API Gateway HTTP API | $1 - $4 |
| Cognito | $2 - $10 |
| S3, CloudWatch, Secrets Manager, EventBridge, alarms | $24 - $48 |
| **Total** | **$115 - $200 / month** |

#### Scenario B: Expected production footprint

- 150 to 500 authenticated users
- 5 to 10 million API requests per month
- 100 GB Aurora storage
- average Aurora utilization around 1.5 ACU

| Component | Monthly estimate |
| --- | ---: |
| Aurora Serverless compute | $156 - $198 |
| Aurora storage and I/O | $24 - $54 |
| Lambda | $6 - $24 |
| API Gateway HTTP API | $6 - $14 |
| Cognito | $4 - $20 |
| S3, CloudWatch, Secrets Manager, EventBridge, alarms | $36 - $72 |
| **Total** | **$232 - $382 / month** |

#### Scenario C: Heavy internal usage

- 500 to 1,500 authenticated users
- 20 million API requests per month
- 200 GB Aurora storage
- average Aurora utilization around 3 ACU

| Component | Monthly estimate |
| --- | ---: |
| Aurora Serverless compute | $312 - $384 |
| Aurora storage and I/O | $54 - $108 |
| Lambda | $18 - $48 |
| API Gateway HTTP API | $22 - $30 |
| Cognito | $8 - $40 |
| S3, CloudWatch, Secrets Manager, EventBridge, alarms | $48 - $96 |
| **Total** | **$462 - $706 / month** |

### Phase 1 annual framing (GovCloud-adjusted)

- prod only, lean to expected usage: about $1.4K to $4.6K per year
- prod only, heavier internal usage: about $5.5K to $8.5K per year

If lower environments run all month, use the database floor as the main multiplier. A lightweight dev or test environment usually adds about $60 to $110 per month each in GovCloud.

---

## Phase 2 and Phase 3 Ballpark (GovCloud-adjusted)

These phases assume live consumption from state applications and websites.

### Phase 2: Live API access for state websites (still mostly table-centric)

Assumptions:

- 50 to 150 million API requests per month
- 300 to 800 GB Aurora storage
- average Aurora utilization around 6 to 10 ACU
- moderate repeat traffic from web properties
- light caching introduced where useful

| Component | Monthly estimate |
| --- | ---: |
| Aurora Serverless compute | $630 - $1,056 |
| Aurora storage and I/O | $144 - $420 |
| Lambda API and jobs | $180 - $600 |
| API Gateway HTTP API | $60 - $168 |
| Cognito, auth, token operations | $12 - $96 |
| Observability, S3, secrets, schedulers, misc | $120 - $300 |
| Optional cache tier | $0 - $300 |
| **Total** | **$1,146 - $2,940 / month** |

Phase 2 annual range:

- production only: about $13K to $35K per year
- production plus scaled lower environments: roughly $22K to $54K per year

### Phase 3: Live multi-state WID backend

Assumptions:

- 250 million to 1 billion API requests per month
- 1 to 3 TB Aurora storage
- average Aurora utilization around 12 to 30 ACU
- higher query concurrency and broader read patterns
- stronger reliability requirements

| Component | Monthly estimate |
| --- | ---: |
| Aurora Serverless compute | $1,260 - $3,180 |
| Aurora storage and I/O | $480 - $1,680 |
| Lambda API and jobs | $840 - $3,600 |
| API Gateway HTTP API | $276 - $1,200 |
| Cognito and auth operations | $120 - $720 |
| Observability, S3, secrets, schedulers, misc | $300 - $1,080 |
| Cache tier and read-scaling support | $180 - $1,200 |
| **Total** | **$3,456 - $12,660 / month** |

Phase 3 annual range:

- production only: about $42K to $152K per year
- production plus realistic lower environments: about $66K to $240K+ per year

---

## GovCloud Modeled Footprints (Prod + Dev)

This section converts the phase ranges into explicit two-environment planning envelopes.

Method used:

- production values use the phase tables above
- dev database capacity is reduced relative to production
- dev serverless usage is significantly lower than production
- shared operational services (logs, secrets, schedulers, artifact storage) are reduced, but not near zero

Dev-sizing assumptions used for these envelopes:

- Aurora and database I/O in dev: generally 25% to 60% of production depending on phase
- Lambda/API/auth traffic in dev: generally 8% to 25% of production
- shared ops in dev: generally 30% to 60% of production

### Footprint 1: Minimum production footprint (Phase 1 lean)

| Environment | Monthly range | Annual range |
| --- | ---: | ---: |
| Production | $115 - $200 | $1.4K - $2.4K |
| Development | $70 - $130 | $0.8K - $1.6K |
| **Combined** | **$185 - $330** | **$2.2K - $4.0K** |

### Footprint 2: Expected production footprint (Phase 2 live usage)

| Environment | Monthly range | Annual range |
| --- | ---: | ---: |
| Production | $1,146 - $2,940 | $13.8K - $35.3K |
| Development | $400 - $1,000 | $4.8K - $12.0K |
| **Combined** | **$1,546 - $3,940** | **$18.6K - $47.3K** |

### Footprint 3: High-adoption production footprint (Phase 3 live backend)

| Environment | Monthly range | Annual range |
| --- | ---: | ---: |
| Production | $3,456 - $12,660 | $41.5K - $151.9K |
| Development | $700 - $3,900 | $8.4K - $46.8K |
| **Combined** | **$4,156 - $16,560** | **$49.9K - $198.7K** |

Notes for implementation planning:

- These dev ranges assume dev has lower request volume than prod, which mainly affects Lambda and API Gateway.
- If dev uses capped Aurora capacity or smaller dedicated compute for specific services, it will stay toward the lower half of these ranges.
- If dev is used for performance validation or heavy integration testing, costs can move toward the upper end quickly.

---

## State Customization Path: Multi-Schema Option

WID allows optional tables and state-specific extensions. If states upload their own data and extend structures, a single shared schema may not be enough.

One workable model:

- canonical shared schema for nationally comparable tables
- state-specific schemas for optional and custom tables
- API surface split between canonical endpoints and state-scoped endpoints
- per-state schema migration/version controls

Operational impact:

- more migration logic and testing
- additional metadata/catalog management
- more complex onboarding and validation workflow per state

Cost impact:

- higher database storage and I/O
- more ETL/runtime variance by state
- higher orchestration and support effort

Budget effect:

- Phase 2 generally moves toward the upper half of its range
- Phase 3 can exceed midpoint and, with broad customization, can exceed the current top-end estimate
- use a contingency of +20% to +40% above the GovCloud baseline Phase 2/3 numbers until real state customization patterns are known

---

## Runtime Choice: Lambda and Containers

For this project shape, API compute should start on Lambda.

Reasons:

- bursty traffic pattern
- low idle cost
- straightforward integration with API Gateway and Cognito

Use Fargate selectively for scheduled workloads that do not fit Lambda limits (runtime length, memory profile, heavy native dependencies).

Avoid an always-on container API in early phases unless there is measured need. It adds fixed runtime cost before traffic requires it.

---

## Shared ULMITA Infrastructure Reuse

The cost tables in this document are conservative gross ranges. In practice, this project is expected to reuse parts of the existing ULMITA GovCloud platform.

Likely reuse areas:

- existing VPC foundation and network controls
- existing NAT gateway footprint
- existing shared logging, metrics, and alert routing
- existing IAM patterns, account baselines, and CI/CD controls
- existing secrets and operational governance patterns

Practical effect:

- lowers net-new infrastructure spend versus a greenfield build
- avoids duplicate platform engineering effort
- improves speed to delivery because baseline controls are already in place

Planning treatment:

- keep the phase tables as gross planning envelopes
- apply a shared-infrastructure credit when producing internal budget numbers
- for ULMITA-hosted delivery, a reasonable planning credit is often in the 10% to 25% range versus a stand-alone build managed by another state or an outside vendor

This is one of the clear cost advantages of running the service inside the broader ULMITA ecosystem.

---

## Cost Risks to Track

1. NAT gateway growth from VPC-attached functions that also need internet egress (risk is lower if existing shared NAT capacity can absorb this workload)
2. keeping multiple Aurora clusters warm all month
3. broad use of provisioned concurrency before latency data justifies it
4. early public traffic expansion
5. SMS-based MFA volume

---

## Implementation Direction for the Private Repo

- .NET 8
- infrastructure as code (CDK for .NET, Terraform, or Pulumi)
- API Gateway HTTP API + Lambda for API layer
- Aurora PostgreSQL Serverless v2 (Aurora Standard initially)
- Cognito authorizer with role/group claims
- S3 + EventBridge Scheduler + Lambda for refresh workflows
- optional scheduled Fargate module available for heavy jobs

This keeps early recurring cost low and leaves room for heavier batch runtime where needed.

---

## Budget Summary

- Phase 1: about $250 to $750 per month in most expected scenarios
- Phase 2: about $1.1K to $3K per month
- Phase 3: about $3.5K to $12.7K per month, with potential to exceed that under broad multi-state customization and higher availability targets
- For planning with both production and development environments, use the combined envelopes in the "GovCloud Modeled Footprints (Prod + Dev)" section
- For internal ULMITA budgeting, apply a shared-infrastructure credit (commonly 10% to 25%) to estimate net-new spend