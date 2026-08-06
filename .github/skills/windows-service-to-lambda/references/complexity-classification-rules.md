# Reference: Complexity Classification Rules

This reference defines the exact rules the agent applies during Skill 01 (Service Inventory)
to assign a complexity grade of **Low**, **Medium**, or **High** to each Windows Service.

Rules are evaluated in order. The **first matching tier** wins — a service meeting any High
criterion is always High regardless of other Low/Medium signals.

---

## High Complexity — Automatic (Any Criterion Triggers High)

| # | Criterion | Detection Signal | Rationale |
|---|-----------|-----------------|-----------|
| H1 | P/Invoke or COM interop | `[DllImport]`, `[ComImport]`, `[Guid]` attributes; `System.Runtime.InteropServices` namespace | Windows-only native bindings; no Lambda equivalent |
| H2 | Named pipe communication | `System.IO.Pipes.NamedPipeServerStream` or `NamedPipeClientStream` in service code | Requires caller redesign as SQS; cannot be automated |
| H3 | MSDTC distributed transaction | `new TransactionScope(TransactionScopeOption.Required)` with multiple connection enlistments; `System.Transactions.Transaction.Current` with multiple `SqlConnection` objects opened inside scope | Lambda cannot enlist in DTC; compensating transaction required |
| H4 | Linked-server query | `OPENQUERY(`, `[server].[db].[schema].[table]` in SQL command text | Cross-server query routing cannot be replicated in Lambda without redesign |
| H5 | MSMQ dependency | `System.Messaging.MessageQueue` | MSMQ has no cloud-native Lambda equivalent; requires SQS architectural redesign |
| H6 | Raw TCP/UDP socket listener | `System.Net.Sockets.TcpListener`, `UdpClient` in `OnStart` | Lambda is not a persistent socket server; requires API Gateway or ALB redesign |
| H7 | .NET Remoting | `MarshalByRefObject`, `RemotingConfiguration`, `RemotingServices` | Deprecated in .NET Core/8; requires WCF or REST redesign |

---

## Medium Complexity — (Any Criterion Triggers Medium, unless already High)

| # | Criterion | Detection Signal | Rationale |
|---|-----------|-----------------|-----------|
| M1 | EventLog writes | `EventLog.WriteEntry(...)` calls | Replacement is straightforward (CloudWatch Logs) but requires file edits |
| M2 | Registry reads | `Registry.GetValue(...)`, `Registry.LocalMachine.OpenSubKey(...)` | Replacement is SSM Parameter Store; straightforward but requires file edits |
| M3 | Many stored procedures | > 3 distinct stored procedure names in `CommandText` assignments | Higher data-access complexity; requires careful parity testing |
| M4 | DataSet / DataTable manipulation | `new DataSet()`, `new DataTable()`, `DataAdapter.Fill(dataSet)` | Works on Lambda but requires review; `DataSet` serialisation behaviour differs slightly on .NET 10 |
| M5 | Database transaction | `conn.BeginTransaction()`, `new SqlTransaction` | Transactions work in Lambda but must be verified for correctness (no rollback on Lambda timeout) |
| M6 | SqlBulkCopy | `new SqlBulkCopy(...)` | Timeout risk; batch pagination assessment required |
| M7 | OnCustomCommand override | `protected override void OnCustomCommand(int command)` | Must be mapped to EventBridge custom events; requires developer review of command semantics |
| M8 | Multiple timer instances | > 1 `System.Timers.Timer` or `System.Threading.Timer` in the same service class | Multiple triggers require multiple Lambda functions or a fan-out pattern |
| M9 | Explicit thread management | `new Thread(...)`, `ThreadPool.QueueUserWorkItem(...)` | Threads are valid in Lambda but must not exceed the function timeout |

---

## Low Complexity — (All remaining services that do not meet M or H criteria)

| Criterion | Description |
|-----------|-------------|
| Single timer trigger | Exactly one `System.Timers.Timer` or `System.Threading.Timer` |
| Primitive config only | Only `ConfigurationManager.AppSettings` and `ConfigurationManager.ConnectionStrings` (no Registry, no custom config sections) |
| 1–3 stored procedures | Simple ADO.NET with up to 3 SP calls; no DataSet, no bulk copy |
| No Windows API surface | No `EventLog`, `Registry`, `ServiceController`, P/Invoke, or COM interop |
| No distributed transactions | No `TransactionScope` with multiple connections; local `BeginTransaction` is allowed |
| Short-running batch | Total worst-case runtime < 5 minutes (well within Lambda 15-min timeout) |

---

## Timeout Risk Assessment (Independent of Complexity Grade)

The timeout risk flag is independent of complexity grade — a Low-complexity service can
still have a timeout risk.

**Flag as `timeoutRisk: true` if any of the following are found:**

| Signal | Worst-Case Estimate |
|--------|-------------------|
| `SqlBulkCopy` with no explicit `BatchSize` limit | Unknown; flag as risk |
| `SqlBulkCopy.BulkCopyTimeout` > 600 seconds | Confirmed risk |
| `SELECT` without `TOP N` / `FETCH NEXT` clause on a table likely to have > 50,000 rows | Risk if per-row processing time > 18ms |
| `Thread.Sleep(ms)` total duration inside a single invocation > 60,000ms (1 minute) | Risk if sleep is in a loop |
| Explicit comment in service source indicating long runtime (e.g. `// runs ~20 mins on month-end`) | Confirmed risk |

**Timeout Risk Mitigation Options (recorded in migration-plan.json):**

| Option | When to Use |
|--------|------------|
| `sqs-pagination` | Batch can be split into equal chunks; each Lambda invocation processes one chunk |
| `step-functions` | Batch requires coordination between multiple processing stages; cannot be chunked simply |
| `increase-batch-filter` | A `WHERE` clause can reduce the dataset per invocation (e.g. filter by date range) |

---

## Override Rules

The user may override a complexity grade during the Skill 02 interview under the following
conditions only:

| Override | Condition |
|---------|----------|
| High → Medium | All H-criterion blockers have been resolved by the developer before handler generation |
| Medium → Low | Developer confirms that M-criterion items are trivial and has reviewed them |
| Low → High | Developer identifies a manual risk not detected by automated scanning |

All overrides must be recorded with a rationale in `migration-plan.json →
servicePlans[].complexityOverrideRationale`.
