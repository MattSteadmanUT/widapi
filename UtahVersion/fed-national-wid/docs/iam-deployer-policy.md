# IAM Policy for the Deploying Identity

No such policy document existed anywhere in the original handoff — the deployment docs only ever
described the needed permissions in prose ("permissions to create VPCs, Aurora clusters, Lambda
functions..."). For a state AWS account with a security team, that's not enough to get access
provisioned. This is a starting point, derived directly from reading every resource
`cloud-deployment/lambda.template` creates — **not a guaranteed-complete or already-reviewed
policy**. Have NC's own AWS security team review and tighten it (especially the `Resource: "*"`
entries, several of which could be scoped further with more effort) before granting it for real.

## What the deploying identity needs, and why

| Service | Why |
|---|---|
| CloudFormation | `dotnet lambda deploy-serverless` deploys the SAM template as a CloudFormation stack |
| IAM | The template creates 3 roles (`ApiExecutionRole`, `IngestionExecutionRole`, `IngestionSchedulerRole`) and needs `PassRole` to hand them to Lambda/EventBridge Scheduler |
| RDS | Creates the Aurora Serverless v2 cluster, instance, and subnet group |
| Lambda | Creates/updates both Lambda functions |
| API Gateway | Creates the HTTP API, JWT authorizer, routes, integrations, stage |
| EventBridge Scheduler | Creates the 7 dataset-group schedules |
| Secrets Manager | Creates the Aurora master password secret |
| SSM Parameter Store | Creates the DB connection string parameter |
| EC2 (read-only) | The template takes an existing VPC/subnets/security group as *input* parameters (it doesn't create networking) — the deploy tooling still needs to describe them to validate the template |
| S3 | `dotnet lambda deploy-serverless` uploads the build artifact to the deployment bucket from Step 3 of [first-deployment-playbook.md](./first-deployment-playbook.md) |
| STS | Routine caller-identity checks most AWS tooling performs |

## Starting-point policy

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "CloudFormationStackLifecycle",
      "Effect": "Allow",
      "Action": [
        "cloudformation:CreateStack",
        "cloudformation:UpdateStack",
        "cloudformation:DeleteStack",
        "cloudformation:DescribeStacks",
        "cloudformation:DescribeStackEvents",
        "cloudformation:DescribeStackResource",
        "cloudformation:DescribeStackResources",
        "cloudformation:GetTemplate",
        "cloudformation:ValidateTemplate",
        "cloudformation:CreateChangeSet",
        "cloudformation:DescribeChangeSet",
        "cloudformation:ExecuteChangeSet",
        "cloudformation:DeleteChangeSet",
        "cloudformation:ListStacks",
        "cloudformation:ListStackResources"
      ],
      "Resource": "*"
    },
    {
      "Sid": "IamRoleManagementScopedToThisProject",
      "Effect": "Allow",
      "Action": [
        "iam:CreateRole",
        "iam:DeleteRole",
        "iam:GetRole",
        "iam:PutRolePolicy",
        "iam:DeleteRolePolicy",
        "iam:GetRolePolicy",
        "iam:AttachRolePolicy",
        "iam:DetachRolePolicy",
        "iam:TagRole",
        "iam:PassRole"
      ],
      "Resource": "arn:*:iam::*:role/*-national-wid-*-role"
    },
    {
      "Sid": "RdsAuroraCluster",
      "Effect": "Allow",
      "Action": [
        "rds:CreateDBCluster",
        "rds:ModifyDBCluster",
        "rds:DeleteDBCluster",
        "rds:DescribeDBClusters",
        "rds:CreateDBInstance",
        "rds:ModifyDBInstance",
        "rds:DeleteDBInstance",
        "rds:DescribeDBInstances",
        "rds:CreateDBSubnetGroup",
        "rds:ModifyDBSubnetGroup",
        "rds:DeleteDBSubnetGroup",
        "rds:DescribeDBSubnetGroups",
        "rds:AddTagsToResource",
        "rds:ListTagsForResource"
      ],
      "Resource": "*"
    },
    {
      "Sid": "LambdaFunctions",
      "Effect": "Allow",
      "Action": [
        "lambda:CreateFunction",
        "lambda:UpdateFunctionCode",
        "lambda:UpdateFunctionConfiguration",
        "lambda:DeleteFunction",
        "lambda:GetFunction",
        "lambda:GetFunctionConfiguration",
        "lambda:ListFunctions",
        "lambda:AddPermission",
        "lambda:RemovePermission",
        "lambda:TagResource",
        "lambda:PublishVersion"
      ],
      "Resource": "*"
    },
    {
      "Sid": "ApiGatewayHttpApi",
      "Effect": "Allow",
      "Action": "apigateway:*",
      "Resource": "*"
    },
    {
      "Sid": "EventBridgeSchedulerRules",
      "Effect": "Allow",
      "Action": [
        "scheduler:CreateSchedule",
        "scheduler:UpdateSchedule",
        "scheduler:DeleteSchedule",
        "scheduler:GetSchedule",
        "scheduler:ListSchedules",
        "scheduler:TagResource"
      ],
      "Resource": "*"
    },
    {
      "Sid": "SecretsManagerDbPassword",
      "Effect": "Allow",
      "Action": [
        "secretsmanager:CreateSecret",
        "secretsmanager:UpdateSecret",
        "secretsmanager:DeleteSecret",
        "secretsmanager:DescribeSecret",
        "secretsmanager:GetSecretValue",
        "secretsmanager:TagResource"
      ],
      "Resource": "*"
    },
    {
      "Sid": "ParameterStore",
      "Effect": "Allow",
      "Action": [
        "ssm:PutParameter",
        "ssm:GetParameter",
        "ssm:GetParameters",
        "ssm:DeleteParameter",
        "ssm:AddTagsToResource"
      ],
      "Resource": "*"
    },
    {
      "Sid": "Ec2NetworkingReadOnly",
      "Effect": "Allow",
      "Action": [
        "ec2:DescribeVpcs",
        "ec2:DescribeSubnets",
        "ec2:DescribeSecurityGroups",
        "ec2:DescribeAvailabilityZones",
        "ec2:CreateNetworkInterface",
        "ec2:DescribeNetworkInterfaces",
        "ec2:DeleteNetworkInterface"
      ],
      "Resource": "*"
    },
    {
      "Sid": "DeploymentArtifactBucket",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:*:s3:::YOUR-DEPLOYMENT-BUCKET-NAME",
        "arn:*:s3:::YOUR-DEPLOYMENT-BUCKET-NAME/*"
      ]
    },
    {
      "Sid": "RoutineIdentityChecks",
      "Effect": "Allow",
      "Action": "sts:GetCallerIdentity",
      "Resource": "*"
    }
  ]
}
```

## Notes on the broad grants above

- `ec2:CreateNetworkInterface`/`DeleteNetworkInterface` are included because VPC-attached Lambdas
  need ENIs created in the target subnets at deploy/invoke time — this is a genuinely common,
  hard-to-avoid broad grant for any VPC-Lambda deployment, not something specific to this project.
- `apigateway:*` is granted broadly because API Gateway's IAM action model doesn't map cleanly onto
  the specific HttpApi/authorizer/route resources this template creates without significant extra
  research — a security team that wants this tightened further should expect to spend real time on
  it specifically, this wasn't skipped casually.
- Replace `YOUR-DEPLOYMENT-BUCKET-NAME` with the actual bucket name from
  [first-deployment-playbook.md](./first-deployment-playbook.md) Step 3.
- The `arn:*:iam::*:role/*-national-wid-*-role` scoping on the `IamRoleManagementScopedToThisProject`
  statement matches this template's actual role-naming pattern
  (`${appEnvironment}-national-wid-api-role`, `${appEnvironment}-national-wid-ingestion-role`,
  `${appEnvironment}-national-wid-scheduler-role`) — if NC renames these in the template, update
  this scoping to match, or it'll silently stop working.
