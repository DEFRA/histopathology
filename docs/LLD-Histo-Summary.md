# LLD Generation Summary — Histopathology

Generation date: 2026-08-25  
Script: `scripts/generate-lld-v2.ps1`  
Output: `docs/LLD-Histo-Populated.docx` (173 KB — all 9 placeholder tokens replaced, 34 sections populated, 24 tables populated, 4 figures described)  
Source files: `LLD-Docs/Histopathology HLSA Presentation v3.pptx` and `LLD-Docs/Histopathology_HLD v0.6.docx`  
Template: `LLD-Docs/CCoE Low Level Design Template.docx` (unmodified — 161 KB)

Generation note: AI-assisted — reviewed by [NEEDS INPUT: Reviewer name], [NEEDS INPUT: Date]

> **IMPORTANT:** This document is AI-assisted and must be reviewed and approved by a responsible individual before use as a design authority. Mark `[NEEDS INPUT]` items must be completed. `[MANUAL REVIEW REQUIRED]` items must be reviewed by a domain expert. Per Defra AI Toolkit guidance — no AI-generated LLD is published or shared without explicit human sign-off.

---

## Migration Details

The database migration uses a Backup and Restore approach from on-premises `DEFACPVWPSQL002\Histology` (SQL Server 2022) to Azure SQL Database. The ATOS Database Team performs the final backup; the DevOps/Migration Team restores to Azure SQL Database.

**Source:** `DEFACPVWPSQL002\Histology` — SQL Server 2022, on-premises, PRODUCTION server  
**Target:** Azure SQL Database — General Purpose Serverless Gen5, 8 vCores max, 200 GB max storage, Zone-Redundant (ZRS), Private Endpoint only  
**Approach:** Backup and Restore (ATOS team takes backup; DevOps/Migration team restores from Azure Storage Account to Azure SQL Database)  
**Migration window:** Planned downtime — minimal downtime until final cutover  
**Pre-cutover requirements (at least 5 days before window):** Infrastructure provisioned, private endpoints configured, Key Vault updated with new connection strings, Change Request approved  
**Go/No-Go checkpoint:** Solution Architect reviews validation results post-restore before authorising user cutover  
**Rollback strategy:** Revert to on-premises application and database; re-migrate in next approved migration window  
**Migration lead:** DevOps/Migration Team  
**DB backup lead:** ATOS Database Team  

Full migration activity plan: Table 11 in LLD-Histo-Populated.docx  
(per HLD Section 5 Migration)

---

## Document Generation Summary

### Sections Auto-Filled

- Cover page: `<Project Name>` replaced with `Histopathology`
- Section 1.1 Purpose of the Low Level Solution Design Document
- Section 1.2 Deviations from HLD (references Table 1: DD001 — Container Apps vs App Service Plan)
- Section 1.3 Current Build Deviations from LLD
- Section 2 Conceptual Design overview paragraph
- Section 2.1 Components — `<project component>` replaced with full Azure services list
- Section 2.2 Environments — `<high level overview of environments and purpose>` replaced with 4-environment description
- Section 3 Dataflows — `<Ingress Details>`, `<Egress Details>`, `<External to External>` all replaced
- Figure 1: Conceptual Architecture Diagram — description inserted
- Figure 2: Data Flow Diagram — description inserted
- Section 4.1.1 Physical Architecture (body text)
- Section 4.1.2 Azure VMs/EC2 — N/A with justification
- Section 4.1.1 App Services
- Section 4.1.1 API Management / API Gateway — N/A with justification
- Section 4.1.1 APIs — N/A with justification
- Section 4.1.1 Service Bus / Integration Services — N/A with justification
- Section 4.1.1 Logic Apps, Functions, Lambda (Azure Function App for annual reset job)
- Section 4.1.2.1 VPC/VNET Configuration
- Section 4.1.2.1 VNET/VPC Subnets
- Section 4.1.2.3 Security Groups and NSGs
- Section 4.1.2.4 External Access
- Section 4.1.3.1 Anti-Virus (Microsoft Defender suite)
- Section 4.1.3.2 Vulnerability Scanning (SonarCloud + Defender for Cloud)
- Section 4.1.3.3 SOC Integration (Sentinel + Log Analytics)
- Section 4.1.4.1 Application Registrations (per-environment Entra ID SAML 2.0 app registrations)
- Section 4.1.4.2 Authentication (Entra ID SAML 2.0)
- Section 4.1.4.3 Authorisation (group-based RBAC, PIM for Azure resources)
- Section 4.1.4.4 Logging and Auditing (Log Analytics + Sentinel)
- Section 4.1.4.5 Encryption (TDE at rest, TLS 1.2+ in transit)
- Section 4.1.4.6 Intrusion Detection and Prevention (WAF OWASP + Sentinel + Defender)
- Section 4.1.5.1 Storage Accounts/S3 Buckets
- Section 4.1.5.1 Databases (Azure SQL Database per-environment)
- Section 4.1.6 Backup & Recovery (automated SQL backups, LTR)
- Section 4.1.7 Capacity Management (auto-scale App Service, serverless SQL)
- Section 4.1.8 Clustering / Resilience (zone redundancy within UK South)
- Section 4.1.9 Disaster Recovery (Tier 3, RTO/RPO 8-48h, IaC redeployment)
- Section 4.1.10.1 Infrastructure Monitoring and Alerting (Azure Monitor + Logic Monitor)
- Section 4.1.10.2 Application Monitoring and Alerting (Application Insights, sampling rates)
- Section 4.1.11 Scheduling (Azure Function App annual reset)
- Section 4.1.12 Software Deployment and Management (Bicep IaC + Azure DevOps CI/CD)
- Section 4.1.13 Licensing (open-source and paid Azure services)
- Section 4.2.1 Pre-Production environment
- Section 4.2.2 Dev/Test environments
- Section 4.2.3 Migration Details
- Table — Version History (versions 0.1 through 0.6 plus 1.0 placeholder)
- Table — Approval (3 rows with NEEDS INPUT flags)
- Table — Document References (5 references)
- Table 1: HLD Deviation (DD001)
- Table 2: Build Deviation (N/A)
- Table 3: Server Configuration (4 as-is servers + 2 to-be Azure services)
- Table 4: Interactions (8 network interaction rows)
- Table 5: Authentication (4 environment rows)
- Table 6: Authorisation (3 role groups + NEEDS INPUT flag)
- Table 7: Databases (4 environment rows)
- Table 8: Backup/Recovery (4 backup types)
- Table 9: Infrastructure Monitoring (6 component rows)
- Table 10: Application Monitoring (4 specification rows across 4 environments)
- Table 11: Migration Details (8 activity rows)

---

### Sections Requiring Manual Review

| Section | Reason |
|---------|--------|
| Section 4.1.4.2 Authentication | Confirm MFA policy and Conditional Access configuration with DEFRA IAM team |
| Section 4.1.4.3 Authorisation | Confirm complete role group list with APHA system owners (ref HLD Table 4 and 5) |
| Section 4.1.4.6 Intrusion Detection and Prevention | Confirm WAF rule set version and custom rules with CCoE security team |
| Section 4.1.9 Disaster Recovery | Confirm Tier 3 classification and RTO/RPO with CCoE service tier team |
| Figure 1: Conceptual Architecture Diagram | Replace text description with formal architecture diagram from HLSA/HLD |
| Figure 2: Data Flow Diagram | Replace text description with formal data flow diagram |
| `<network diagram>` placeholder (Section 4.1.1 Physical Architecture) | Insert formal Hub-Spoke network diagram |
| Risk and Open Questions section | Requires human input — not auto-generated |

---

### Needs Input

1. `[NEEDS INPUT: LLD Author Name]` — Version History table row 1.0: name of LLD author
2. `[NEEDS INPUT: Solution Architect name]` — Approval table: Solution Architect approver name
3. `[NEEDS INPUT: TWG Reviewer name]` — Approval table: TWG Reviewer name
4. `[NEEDS INPUT: Date]` (x3) — Approval table: approval/review dates
5. `[NEEDS INPUT: Complete role list from As-Is user role mapping -- per HLD Table 4 and Table 5]` — Table 6 Authorisation: complete application role list from existing system

---

### Assumptions

- HLD v0.6 (Histopathology_HLD v0.6.docx) takes precedence over HLSA v3 where they conflict (DD001: App Service Plan vs Container Apps)
- Four environments (DEV, Test, PreProd, Prod) assumed as per HLD Section 3.3 Table 18
- All Azure services provisioned in UK South primary region only (no secondary region — Tier 3 DR)
- SAML 2.0 via ITfoxtec library is the confirmed authentication mechanism (not OIDC)
- Azure SQL Database uses Serverless General Purpose Gen5 tier based on HLD Table specification; ZRS confirmed for Prod and PreProd only
- Private endpoints are used for all backend services (SQL, Key Vault, Storage) — no public network access
- CCoE-managed Application Gateway WAF is shared infrastructure managed outside this project
- Azure Function App is the confirmed mechanism for the annual Histology Reference reset job
- No external integrations exist in the target architecture; linked-server dependency to TSE_VLA (Daybook) has been removed
- Backup retention of 7-35 days (configurable) per HLD; specific value must be confirmed by the operations team

---

## Confidence by Section

| Section | Confidence | Notes |
|---------|------------|-------|
| Version History | High | Sourced directly from HLD v0.6 version history table |
| Overview of Project | High | Sourced verbatim from HLD Section 1 Introduction |
| Overview of Solution | High | Sourced from HLD Section 1.4 and Section 2.3 |
| Components (2.1) | High | Sourced from HLD Section 2.3.1 Azure Service Inventory |
| Environments (2.2) | High | Sourced from HLD Section 3.3 Table 18 |
| Dataflows — Ingress | High | Sourced from HLD Section 3.3 Tables 19 and 20 |
| Dataflows — Egress | High | Sourced from HLD Section 3.3 Table 19 Egress Path |
| Dataflows — External to External | High | Confirmed no external integrations — HLD Section 1.4.2 and 2.4 |
| Physical Architecture (4.1.1) | High | Sourced from HLSA Slide 12 and HLD Section 2.3 |
| Azure VMs/EC2 (4.1.2) | High | Confirmed N/A — HLD Section 1.4.2 Out of Scope |
| App Services | High | Sourced from HLD App Service Plan Table 7 |
| API Gateway / APIs / Service Bus | High | Confirmed N/A — HLD Section 1.4.2 |
| Logic Apps / Functions | High | Sourced from HLD Section 2.3.1 Table 11 |
| VNet Configuration | High | Sourced from HLD Section 3.3 Table 18 |
| Security Groups and NSGs | High | Sourced from HLD Section 3.1 Security Architecture |
| External Access | High | Sourced from HLD Section 3.2 Table 17 |
| Anti-Virus / Vulnerability Scanning | High | Sourced from HLD Section 3.1 Table 15 and Section 2.6 |
| SOC Integration | High | Sourced from HLD Section 3.1 and Section 4.5 |
| Application Registrations | High | Sourced from HLD Section 3.1 Table 6 |
| Authentication | High | Sourced from HLD Section 3.1 — ITfoxtec SAML 2.0 confirmed |
| Authorisation | Medium | Role groups partially inferred — complete list requires human input |
| Logging and Auditing | High | Sourced from HLD Section 4.5 and 3.1 |
| Encryption | High | TDE and TLS 1.2+ confirmed in HLD Section 3.1 |
| Intrusion Detection | High | WAF + Sentinel + Defender sourced from HLD Section 3.1 Table 15 |
| Storage Accounts | High | Sourced from HLD Section 2.3.1 Table 12 |
| Databases (Table 7) | High | Sourced from HLD Section 3.3 and App Service Table 7 |
| Backup & Recovery (Table 8) | High | Sourced from HLD Section 4.4 |
| Capacity Management | High | Sourced from HLD Section 4.2 and Table 7 |
| Clustering / Resilience | High | Sourced from HLD Section 4.2 Table 24 |
| Disaster Recovery | Medium | Tier 3 classification needs CCoE confirmation — RTO/RPO from HLD Section 4.3 |
| Infrastructure Monitoring (Table 9) | High | Sourced from HLD Section 4.5 Table 26 |
| Application Monitoring (Table 10) | High | Sourced from HLD Section 4.5 monitoring tables |
| Scheduling | High | Sourced from HLD Section 2.3.1 Table 11 |
| Software Deployment | High | Sourced from HLD Section 2.6 DevOps Architecture |
| Licensing | High | Sourced from HLD Section 1.4 and HLSA Slide 21 |
| Pre-Production (4.2.1) | High | Sourced from HLD Section 3.3 Table 18 and App Service Table 7 |
| Dev/Test (4.2.2) | High | Sourced from HLD Section 3.3 Table 18 and App Service Table 7 |
| Migration Details (Table 11) | High | Sourced from HLD Section 5 Migration |
| Figure 1 / Figure 2 | Low | Text descriptions only — formal diagrams require manual creation |
| Approval table | Low | Names and dates require human input |
