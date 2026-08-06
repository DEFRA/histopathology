# Reference: IAM Least-Privilege Policy Reference

This reference documents the minimum IAM permissions required for each Lambda function
migrated from a Windows Service. Permissions are derived from the service's detected
data-access patterns and AWS service dependencies.

---

## Base Permissions (All Functions)

Every generated IAM execution role includes these permissions regardless of other dependencies.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "CloudWatchLogs",
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:<region>:<account>:log-group:/aws/lambda/<function-name>:*"
    },
    {
      "Sid": "SQSDeadLetterQueue",
      "Effect": "Allow",
      "Action": ["sqs:SendMessage"],
      "Resource": "<dlq-arn>"
    }
  ]
}
```

---

## Secrets Manager (Connection Strings and App Secrets)

**Trigger**: Service uses `ConfigurationManager.ConnectionStrings` or sensitive `AppSettings`.

```json
{
  "Sid": "SecretsManagerRead",
  "Effect": "Allow",
  "Action": ["secretsmanager:GetSecretValue"],
  "Resource": [
    "arn:aws:secretsmanager:<region>:<account>:secret:<path-prefix><function-name>/*"
  ]
}
```

**Scope**: Resource ARN is scoped to the function's path prefix only. Never use `"Resource": "*"`.

---

## SSM Parameter Store (Non-Sensitive Config)

**Trigger**: Service uses `ConfigurationManager.AppSettings` with non-sensitive values
migrated to SSM Parameter Store.

```json
{
  "Sid": "SSMParameterRead",
  "Effect": "Allow",
  "Action": [
    "ssm:GetParameter",
    "ssm:GetParameters",
    "ssm:GetParametersByPath"
  ],
  "Resource": [
    "arn:aws:ssm:<region>:<account>:parameter/<path-prefix><function-name>/*"
  ]
}
```

---

## SQS Queue (Batch Pagination Pattern)

**Trigger**: Service uses SQS pagination for batch processing (chunked via SQS).

```json
{
  "Sid": "SQSQueueAccess",
  "Effect": "Allow",
  "Action": [
    "sqs:ReceiveMessage",
    "sqs:DeleteMessage",
    "sqs:GetQueueAttributes",
    "sqs:ChangeMessageVisibility"
  ],
  "Resource": "<input-queue-arn>"
}
```

---

## S3 (File-Processing Pattern)

**Trigger**: Service uses `FileSystemWatcher` (migrated to S3 Event Notification trigger).

```json
{
  "Sid": "S3BucketAccess",
  "Effect": "Allow",
  "Action": [
    "s3:GetObject",
    "s3:DeleteObject"
  ],
  "Resource": "arn:aws:s3:::<bucket-name>/<prefix>/*"
}
```

---

## RDS Data API (if using Aurora Serverless v2)

**Trigger**: Service accesses Aurora Serverless v2 via the Data API instead of direct TCP.

```json
{
  "Sid": "RDSDataAPI",
  "Effect": "Allow",
  "Action": [
    "rds-data:ExecuteStatement",
    "rds-data:BatchExecuteStatement",
    "rds-data:BeginTransaction",
    "rds-data:CommitTransaction",
    "rds-data:RollbackTransaction"
  ],
  "Resource": "arn:aws:rds:<region>:<account>:cluster:<cluster-name>"
}
```

---

## VPC Access (Lambda inside VPC)

**Trigger**: `vpcEnabled: true` in migration plan.

Managed policy — attach via `aws_iam_role_policy_attachment` (Terraform) or
`ManagedPolicyArns` (CloudFormation):

```
arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole
```

This policy grants `ec2:CreateNetworkInterface`, `ec2:DescribeNetworkInterfaces`,
and `ec2:DeleteNetworkInterface` — required for Lambda to attach to VPC subnets.

---

## DynamoDB (Cursor Tracking for Paginated Batches)

**Trigger**: Service uses SQS pagination with DynamoDB for cursor tracking.

```json
{
  "Sid": "DynamoDBCursorTracking",
  "Effect": "Allow",
  "Action": [
    "dynamodb:GetItem",
    "dynamodb:PutItem",
    "dynamodb:UpdateItem"
  ],
  "Resource": "arn:aws:dynamodb:<region>:<account>:table/<cursor-table-name>"
}
```

---

## Policy Construction Rules

1. **Never use `"Resource": "*"`** — all resource ARNs must be scoped to the specific
   function's resources.
2. **Separate statements per service** — do not combine permissions for multiple Lambda
   functions in one policy.
3. **Secrets Manager ARN must include the secret suffix** — Secrets Manager ARNs end with
   a random 6-character suffix (e.g. `secret:MySecret-aBcDeF`). Use a wildcard on the suffix:
   `arn:aws:secretsmanager:<region>:<account>:secret:<path>*`
4. **No `iam:PassRole` unless Step Functions is used** — only add if the function needs to
   pass a role to another AWS service.
5. **No `s3:*` or `sqs:*` wildcards** — use the minimum action set required.
