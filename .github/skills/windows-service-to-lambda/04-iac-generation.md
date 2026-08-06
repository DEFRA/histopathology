# Playbook 04: IaC Generation

**Purpose**: Generate complete Infrastructure-as-Code for every Lane 1 and Lane 2 service
in the migration plan. Covers EventBridge Scheduler, Lambda function, IAM execution role,
SQS Dead Letter Queue, Secrets Manager references, CloudWatch Log Group, and CloudWatch
metric alarms. Output is either Terraform HCL or CloudFormation YAML — never both for the
same service.

**Input**: `migration-output/migration-plan.json`, generated handler metadata  
**Output**: `migration-output/terraform/<ServiceName>/` or `migration-output/cloudformation/<ServiceName>/`

---

## Step 1 — Read IaC Toolchain Decision

Load `migration-plan.json` → `decisions.iacToolchain`.
- If `"terraform"`: execute Steps 2–4 (Terraform HCL).
- If `"cloudformation"`: execute Steps 5–7 (CloudFormation YAML).

Produce IaC for every service in `servicePlans` where `lane` is `1` or `2`.

---

## Step 2 — Terraform: variables.tf

For each service, create `migration-output/terraform/<ServiceName>/variables.tf`:

```hcl
# ─── Variables for <ServiceName> Lambda function ─────────────────────────────
# MIGRATED FROM: Windows Service <ServiceName> (<OriginalProjectFile>)

variable "aws_region" {
  description = "AWS region for all resources"
  type        = string
  default     = "<awsRegion>"
}

variable "environment" {
  description = "Deployment environment (dev, tst, uat, prd)"
  type        = string
}

variable "lambda_timeout_seconds" {
  description = "Lambda function timeout in seconds"
  type        = number
  default     = <timeoutSeconds>
}

variable "lambda_memory_mb" {
  description = "Lambda function memory allocation in MB"
  type        = number
  default     = <memorySizeMb>
}

variable "dlq_retention_days" {
  description = "Number of days SQS DLQ retains messages"
  type        = number
  default     = <dlqRetentionDays>
}

variable "dlq_alert_threshold" {
  description = "Number of DLQ messages before CloudWatch alarm fires"
  type        = number
  default     = <dlqAlertThreshold>
}

variable "schedule_expression" {
  description = "EventBridge Scheduler expression"
  # MIGRATED FROM: System.Timers.Timer interval <intervalMs>ms
  type        = string
  default     = "<scheduleExpression>"
}

variable "schedule_enabled" {
  description = "Whether the EventBridge schedule rule is active"
  type        = bool
  default     = <scheduleEnabled>
}

# VPC variables — only used when vpc_enabled = true
variable "vpc_enabled" {
  description = "Deploy Lambda inside a VPC"
  type        = bool
  default     = <vpcEnabled>
}

variable "subnet_ids" {
  description = "Subnet IDs for VPC-enabled Lambda"
  type        = list(string)
  default     = []
}

variable "security_group_ids" {
  description = "Security group IDs for VPC-enabled Lambda"
  type        = list(string)
  default     = []
}
```

---

## Step 3 — Terraform: main.tf

Create `migration-output/terraform/<ServiceName>/main.tf`:

```hcl
# ─── <ServiceName> Lambda Migration ──────────────────────────────────────────
# MIGRATED FROM: Windows Service <ServiceName> (<OriginalProjectFile>)
# Original trigger: <OriginalTriggerDescription>
# Migration date: <YYYY-MM-DD>

terraform {
  required_version = ">= 1.6.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  function_name = "<lambdaFunctionName>-${var.environment}"
  common_tags = {
    MigratedFrom = "<ServiceName>"
    MigrationDate = "<YYYY-MM-DD>"
    Environment  = var.environment
  }
}

# ── SQS Dead Letter Queue ─────────────────────────────────────────────────────
resource "aws_sqs_queue" "dlq" {
  name                      = "${local.function_name}-dlq"
  message_retention_seconds = var.dlq_retention_days * 86400
  tags                      = local.common_tags
}

# ── IAM Execution Role ────────────────────────────────────────────────────────
# MIGRATED FROM: Windows Service logon account (typically NETWORK SERVICE or a domain account)
resource "aws_iam_role" "lambda_exec" {
  name = "${local.function_name}-exec-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "lambda.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })

  tags = local.common_tags
}

resource "aws_iam_role_policy" "lambda_exec_policy" {
  name = "${local.function_name}-exec-policy"
  role = aws_iam_role.lambda_exec.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      # CloudWatch Logs — structured logging (replaces EventLog)
      {
        Effect   = "Allow"
        Action   = ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"]
        Resource = "arn:aws:logs:${var.aws_region}:*:log-group:/aws/lambda/${local.function_name}:*"
      },
      # SQS DLQ — send failed invocation records
      {
        Effect   = "Allow"
        Action   = ["sqs:SendMessage"]
        Resource = aws_sqs_queue.dlq.arn
      },
      # Secrets Manager — connection string retrieval (replaces ConfigurationManager)
      # MIGRATED FROM: ConfigurationManager.ConnectionStrings[<connectionStringKeys>]
      {
        Effect   = "Allow"
        Action   = ["secretsmanager:GetSecretValue"]
        Resource = [
          # Add one ARN per secret referenced in this function
          # "arn:aws:secretsmanager:${var.aws_region}:*:secret:<secretsPathPrefix><ServiceName>/*"
        ]
      }
    ]
  })
}

# Attach AWS managed policy for VPC access (only included when VPC-enabled)
resource "aws_iam_role_policy_attachment" "vpc_access" {
  count      = var.vpc_enabled ? 1 : 0
  role       = aws_iam_role.lambda_exec.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# ── CloudWatch Log Group ──────────────────────────────────────────────────────
# MIGRATED FROM: Windows Event Log (Application log, source "<ServiceName>")
resource "aws_cloudwatch_log_group" "lambda_logs" {
  name              = "/aws/lambda/${local.function_name}"
  retention_in_days = 30
  tags              = local.common_tags
}

# ── Lambda Function ───────────────────────────────────────────────────────────
resource "aws_lambda_function" "service" {
  function_name = local.function_name
  role          = aws_iam_role.lambda_exec.arn
  runtime       = "<lambdaRuntime>"   # dotnet10
  handler       = "<handler>"         # from migration-plan.json
  timeout       = var.lambda_timeout_seconds
  memory_size   = var.lambda_memory_mb
  filename      = "<deployment_package_path>"  # CI/CD pipeline populates this

  dead_letter_config {
    target_arn = aws_sqs_queue.dlq.arn
  }

  environment {
    variables = {
      # MIGRATED FROM: ConfigurationManager.AppSettings / ConnectionStrings
      # Secret value is sourced at runtime from Secrets Manager — no plaintext here
      MAIN_DB_CONNECTION_SECRET = "<secretsPathPrefix><ServiceName>/MainDbConnection"
      ENVIRONMENT               = var.environment
    }
  }

  dynamic "vpc_config" {
    for_each = var.vpc_enabled ? [1] : []
    content {
      subnet_ids         = var.subnet_ids
      security_group_ids = var.security_group_ids
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.lambda_logs,
    aws_iam_role_policy.lambda_exec_policy
  ]

  tags = local.common_tags
}

# ── EventBridge Scheduler ─────────────────────────────────────────────────────
# MIGRATED FROM: System.Timers.Timer interval <intervalMs>ms
# Original schedule: every <intervalMinutes> minute(s)
resource "aws_cloudwatch_event_rule" "schedule" {
  name                = "${local.function_name}-schedule"
  description         = "Trigger for ${local.function_name} — migrated from Windows Service timer"
  schedule_expression = var.schedule_expression
  is_enabled          = var.schedule_enabled
  tags                = local.common_tags
}

resource "aws_cloudwatch_event_target" "lambda_target" {
  rule      = aws_cloudwatch_event_rule.schedule.name
  target_id = "${local.function_name}-target"
  arn       = aws_lambda_function.service.arn
}

resource "aws_lambda_permission" "allow_eventbridge" {
  statement_id  = "AllowExecutionFromEventBridge"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.service.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.schedule.arn
}

# ── CloudWatch Alarms ─────────────────────────────────────────────────────────
resource "aws_cloudwatch_metric_alarm" "lambda_errors" {
  alarm_name          = "${local.function_name}-errors"
  alarm_description   = "Lambda function error rate exceeds threshold"
  namespace           = "AWS/Lambda"
  metric_name         = "Errors"
  dimensions          = { FunctionName = aws_lambda_function.service.function_name }
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 1
  comparison_operator = "GreaterThanOrEqualToThreshold"
  treat_missing_data  = "notBreaching"
  tags                = local.common_tags
}

resource "aws_cloudwatch_metric_alarm" "dlq_depth" {
  count               = var.dlq_alert_threshold > 0 ? 1 : 0
  alarm_name          = "${local.function_name}-dlq-depth"
  alarm_description   = "DLQ message count indicates failed invocations"
  namespace           = "AWS/SQS"
  metric_name         = "ApproximateNumberOfMessagesVisible"
  dimensions          = { QueueName = aws_sqs_queue.dlq.name }
  statistic           = "Sum"
  period              = 60
  evaluation_periods  = 1
  threshold           = var.dlq_alert_threshold
  comparison_operator = "GreaterThanOrEqualToThreshold"
  treat_missing_data  = "notBreaching"
  tags                = local.common_tags
}
```

---

## Step 4 — Terraform: outputs.tf

```hcl
output "lambda_function_arn" {
  description = "ARN of the migrated Lambda function"
  value       = aws_lambda_function.service.arn
}

output "lambda_function_name" {
  description = "Name of the migrated Lambda function"
  value       = aws_lambda_function.service.function_name
}

output "dlq_arn" {
  description = "ARN of the Dead Letter Queue"
  value       = aws_sqs_queue.dlq.arn
}

output "iam_role_arn" {
  description = "ARN of the Lambda execution role"
  value       = aws_iam_role.lambda_exec.arn
}
```

---

## Step 5 — CloudFormation: template.yaml

> Only generated when `decisions.iacToolchain == "cloudformation"`.

Create `migration-output/cloudformation/<ServiceName>/template.yaml`:

```yaml
AWSTemplateFormatVersion: "2010-09-09"
Description: >
  <ServiceName> Lambda function — migrated from Windows Service <ServiceName>.
  Original trigger: <OriginalTriggerDescription>.
  Migration date: <YYYY-MM-DD>.

Parameters:
  Environment:
    Type: String
    AllowedValues: [dev, tst, uat, prd]
  ScheduleExpression:
    # MIGRATED FROM: System.Timers.Timer interval <intervalMs>ms
    Type: String
    Default: "<scheduleExpression>"
  ScheduleEnabled:
    Type: String
    AllowedValues: ["ENABLED", "DISABLED"]
    Default: "DISABLED"
  LambdaTimeout:
    Type: Number
    Default: <timeoutSeconds>
  LambdaMemory:
    Type: Number
    Default: <memorySizeMb>
  DlqRetentionDays:
    Type: Number
    Default: <dlqRetentionDays>

Resources:

  DeadLetterQueue:
    Type: AWS::SQS::Queue
    Properties:
      QueueName: !Sub "<lambdaFunctionName>-${Environment}-dlq"
      MessageRetentionPeriod: !Mul [!Ref DlqRetentionDays, 86400]

  LambdaExecutionRole:
    # MIGRATED FROM: Windows Service logon account
    Type: AWS::IAM::Role
    Properties:
      RoleName: !Sub "<lambdaFunctionName>-${Environment}-exec-role"
      AssumeRolePolicyDocument:
        Version: "2012-10-17"
        Statement:
          - Effect: Allow
            Principal: { Service: lambda.amazonaws.com }
            Action: sts:AssumeRole
      Policies:
        - PolicyName: LambdaExecPolicy
          PolicyDocument:
            Version: "2012-10-17"
            Statement:
              - Effect: Allow
                Action: [logs:CreateLogGroup, logs:CreateLogStream, logs:PutLogEvents]
                Resource: !Sub "arn:aws:logs:${AWS::Region}:${AWS::AccountId}:log-group:/aws/lambda/<lambdaFunctionName>-${Environment}:*"
              - Effect: Allow
                Action: [sqs:SendMessage]
                Resource: !GetAtt DeadLetterQueue.Arn
              # MIGRATED FROM: ConfigurationManager.ConnectionStrings
              - Effect: Allow
                Action: [secretsmanager:GetSecretValue]
                Resource: !Sub "arn:aws:secretsmanager:${AWS::Region}:${AWS::AccountId}:secret:*"

  LogGroup:
    # MIGRATED FROM: Windows Event Log (Application log, source "<ServiceName>")
    Type: AWS::Logs::LogGroup
    Properties:
      LogGroupName: !Sub "/aws/lambda/<lambdaFunctionName>-${Environment}"
      RetentionInDays: 30

  LambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      FunctionName: !Sub "<lambdaFunctionName>-${Environment}"
      Role: !GetAtt LambdaExecutionRole.Arn
      Runtime: <lambdaRuntime>
      Handler: <handler>
      Timeout: !Ref LambdaTimeout
      MemorySize: !Ref LambdaMemory
      DeadLetterConfig:
        TargetArn: !GetAtt DeadLetterQueue.Arn
      Environment:
        Variables:
          # MIGRATED FROM: ConfigurationManager.AppSettings / ConnectionStrings
          MAIN_DB_CONNECTION_SECRET: !Sub "<secretsPathPrefix><ServiceName>/MainDbConnection"
          ENVIRONMENT: !Ref Environment
    DependsOn: [LogGroup, LambdaExecutionRole]

  ScheduleRule:
    # MIGRATED FROM: System.Timers.Timer interval <intervalMs>ms
    Type: AWS::Events::Rule
    Properties:
      Name: !Sub "<lambdaFunctionName>-${Environment}-schedule"
      Description: "EventBridge trigger for <ServiceName> — migrated from Windows Service timer"
      ScheduleExpression: !Ref ScheduleExpression
      State: !Ref ScheduleEnabled
      Targets:
        - Id: LambdaTarget
          Arn: !GetAtt LambdaFunction.Arn

  LambdaPermission:
    Type: AWS::Lambda::Permission
    Properties:
      FunctionName: !GetAtt LambdaFunction.Arn
      Action: lambda:InvokeFunction
      Principal: events.amazonaws.com
      SourceArn: !GetAtt ScheduleRule.Arn

  ErrorAlarm:
    Type: AWS::CloudWatch::Alarm
    Properties:
      AlarmName: !Sub "<lambdaFunctionName>-${Environment}-errors"
      Namespace: AWS/Lambda
      MetricName: Errors
      Dimensions:
        - Name: FunctionName
          Value: !Ref LambdaFunction
      Statistic: Sum
      Period: 300
      EvaluationPeriods: 1
      Threshold: 1
      ComparisonOperator: GreaterThanOrEqualToThreshold
      TreatMissingData: notBreaching

Outputs:
  LambdaFunctionArn:
    Value: !GetAtt LambdaFunction.Arn
  DLQArn:
    Value: !GetAtt DeadLetterQueue.Arn
  ExecutionRoleArn:
    Value: !GetAtt LambdaExecutionRole.Arn
```

---

## Step 6 — IaC Validation Checks

After generating all IaC files, run the following checks:

**Terraform**:
```bash
cd migration-output/terraform/<ServiceName>
terraform init -backend=false
terraform validate
```

**CloudFormation**:
```bash
aws cloudformation validate-template \
  --template-body file://migration-output/cloudformation/<ServiceName>/template.yaml \
  --region <awsRegion>
```

For each validation error, fix the template and re-validate. Cap at 3 attempts; surface
remaining errors as blocking issues in the migration report.

**Manual checks (all IaC)**:
- No hardcoded AWS account IDs — use `${AWS::AccountId}` (CFn) or data sources (Terraform).
- No hardcoded secret values — only Secrets Manager ARN references or path strings.
- No Windows backslash paths in any IaC file.
- `scheduleEnabled` / `ScheduleEnabled` defaults to `false` / `"DISABLED"` — schedules must be
  explicitly enabled after smoke testing to prevent accidental execution.
