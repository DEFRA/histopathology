# Migration Validation Checklist

> **Migration**: {{SOURCE_PLATFORM}} → {{TARGET_PLATFORM}}
> **Date**: {{DATE}}
> **Validator**: {{VALIDATOR}}

---

## Summary

| Metric       | Count          |
| ------------ | -------------- |
| Total checks | {{TOTAL}}      |
| ✅ Passed    | {{PASS_COUNT}} |
| ❌ Failed    | {{FAIL_COUNT}} |
| ⏭️ Skipped   | {{SKIP_COUNT}} |
| ⚠️ Warnings  | {{WARN_COUNT}} |

**Overall result**: {{PASS / FAIL / CONDITIONAL PASS}}

---

## 1. Row Count Comparison

Source counts frozen at: {{TIMESTAMP}}

| #   | Table / Collection | Source Count | Target Count | Status  | Notes      |
| --- | ------------------ | ------------ | ------------ | ------- | ---------- |
| 1   | {{TABLE_NAME}}     | {{SOURCE}}   | {{TARGET}}   | ✅ PASS |            |
| 2   | {{TABLE_NAME}}     | {{SOURCE}}   | {{TARGET}}   | ❌ FAIL | {{DETAIL}} |

**Totals**:

- Tables compared: {{N}}
- Exact matches: {{N}}
- Within tolerance: {{N}}
- Mismatches: {{N}}

---

## 2. Schema Validation

### 2.1 Table Existence

| #   | Source Table     | Target Table     | Status  | Notes |
| --- | ---------------- | ---------------- | ------- | ----- |
| 1   | {{SOURCE_TABLE}} | {{TARGET_TABLE}} | ✅ PASS |       |

### 2.2 Column Validation

| #   | Table     | Column  | Source Type  | Target Type  | Nullable | Status  |
| --- | --------- | ------- | ------------ | ------------ | -------- | ------- |
| 1   | {{TABLE}} | {{COL}} | {{SRC_TYPE}} | {{TGT_TYPE}} | {{Y/N}}  | ✅ PASS |

### 2.3 Missing or Extra Columns

| #   | Table     | Column  | Issue             | Action     |
| --- | --------- | ------- | ----------------- | ---------- |
| 1   | {{TABLE}} | {{COL}} | Missing on target | {{ACTION}} |

---

## 3. Constraint Validation

### 3.1 Primary Keys

| #   | Table     | PK Columns  | Source    | Target    | Status  |
| --- | --------- | ----------- | --------- | --------- | ------- |
| 1   | {{TABLE}} | {{COLUMNS}} | ✅ Exists | ✅ Exists | ✅ PASS |

### 3.2 Foreign Keys

| #   | Table     | FK Name | Column(s) | References                 | Cascade          | Status  |
| --- | --------- | ------- | --------- | -------------------------- | ---------------- | ------- |
| 1   | {{TABLE}} | {{FK}}  | {{COL}}   | {{REF_TABLE}}({{REF_COL}}) | {{CASCADE_RULE}} | ✅ PASS |

### 3.3 Unique Constraints

| #   | Table     | Constraint/Index | Column(s)   | Status  |
| --- | --------- | ---------------- | ----------- | ------- |
| 1   | {{TABLE}} | {{NAME}}         | {{COLUMNS}} | ✅ PASS |

### 3.4 Check Constraints

| #   | Table     | Constraint | Expression | Status  |
| --- | --------- | ---------- | ---------- | ------- |
| 1   | {{TABLE}} | {{NAME}}   | {{EXPR}}   | ✅ PASS |

---

## 4. Index Validation

| #   | Table     | Index Name | Column(s)   | Type   | Unique | Status  | Notes |
| --- | --------- | ---------- | ----------- | ------ | ------ | ------- | ----- |
| 1   | {{TABLE}} | {{INDEX}}  | {{COLUMNS}} | B-tree | Y/N    | ✅ PASS |       |

**Summary**:

- Indexes on source: {{N}}
- Indexes on target: {{N}}
- Matched: {{N}}
- Missing on target: {{N}}
- Additional on target (from tuning): {{N}}

---

## 5. Sequence / Identity Validation

| #   | Sequence Name | Source Current Value | Target Current Value | Max PK Value | Status  |
| --- | ------------- | -------------------- | -------------------- | ------------ | ------- |
| 1   | {{SEQ}}       | {{SRC_VAL}}          | {{TGT_VAL}}          | {{MAX_PK}}   | ✅ PASS |

**Rule**: Target current value must be ≥ max PK value to prevent conflicts on insert.

---

## 6. Functional Validation

### 6.1 Stored Procedure / Function Tests

| #   | Procedure | Test Input | Source Output | Target Output | Status  |
| --- | --------- | ---------- | ------------- | ------------- | ------- |
| 1   | {{PROC}}  | {{INPUT}}  | {{SRC_OUT}}   | {{TGT_OUT}}   | ✅ PASS |

### 6.2 View Validation

| #   | View     | Sample Query                    | Source Result | Target Result | Status  |
| --- | -------- | ------------------------------- | ------------- | ------------- | ------- |
| 1   | {{VIEW}} | `SELECT COUNT(*) FROM {{VIEW}}` | {{SRC}}       | {{TGT}}       | ✅ PASS |

### 6.3 Trigger Validation

| #   | Trigger     | Table     | Event                    | Test Action | Expected Result | Actual Result | Status  |
| --- | ----------- | --------- | ------------------------ | ----------- | --------------- | ------------- | ------- |
| 1   | {{TRIGGER}} | {{TABLE}} | {{INSERT/UPDATE/DELETE}} | {{ACTION}}  | {{EXPECTED}}    | {{ACTUAL}}    | ✅ PASS |

---

## 7. Data Integrity Spot Checks

Sample specific rows to verify data accuracy (not just counts).

| #   | Table     | Query                                 | Expected  | Actual    | Status  |
| --- | --------- | ------------------------------------- | --------- | --------- | ------- |
| 1   | {{TABLE}} | `SELECT col FROM t WHERE id = {{ID}}` | {{VALUE}} | {{VALUE}} | ✅ PASS |

---

## 8. Outstanding Issues

Issues that require remediation before the migration can be declared successful.

| #   | Category | Table/Object | Issue     | Severity             | Remediation | Owner     |
| --- | -------- | ------------ | --------- | -------------------- | ----------- | --------- |
| 1   | {{CAT}}  | {{OBJECT}}   | {{ISSUE}} | Critical/High/Medium | {{FIX}}     | {{OWNER}} |

---

## Sign-Off

| Role          | Name     | Date     | Approved |
| ------------- | -------- | -------- | -------- |
| DBA           | {{NAME}} | {{DATE}} | ☐        |
| Tech Lead     | {{NAME}} | {{DATE}} | ☐        |
| QA            | {{NAME}} | {{DATE}} | ☐        |
| Product Owner | {{NAME}} | {{DATE}} | ☐        |
