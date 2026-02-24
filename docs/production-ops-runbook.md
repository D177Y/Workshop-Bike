# Production Ops Runbook

## Health Endpoints
- `GET /health/live`
  - Liveness probe.
  - Returns `Healthy` when process is running.
- `GET /health/ready`
  - Readiness probe.
  - Includes database connectivity check.

## Alerting
- Configure in `appsettings` or user secrets:
  - `Operations:EnableAlertEmails=true`
  - `Operations:AlertEmail=<ops mailbox>`
- Alert hooks currently send email for:
  - trial purge background worker failures
  - Stripe subscription reconciliation worker failures

## Stripe Subscription Sync
- Webhook endpoint:
  - `POST /stripe/webhook`
- Supported events:
  - `checkout.session.completed`
  - `customer.subscription.created`
  - `customer.subscription.updated`
  - `customer.subscription.deleted`
  - `invoice.payment_succeeded`
  - `invoice.payment_failed`
- Background reconciliation:
  - Runs every 6 hours.
  - Repairs tenant subscription status drift.

## Backup Procedure
1. Run:
   - `pwsh ./scripts/db-backup.ps1 -Database workshop_dev -Username <user> -Password <pass>`
2. Store generated `.sql` in secure backup storage.
3. Keep at least:
   - daily backups for 14 days
   - weekly backups for 8 weeks
   - monthly backups for 12 months

## Restore Procedure
1. Choose backup file.
2. Restore into a restore target DB first:
   - `pwsh ./scripts/db-restore.ps1 -Database workshop_restore_drill -BackupFile <path.sql> -Username <user> -Password <pass>`
3. Validate:
   - latest migration present
   - tenant/user counts expected
   - random booking/profile checks pass

## Monthly Backup/Restore Drill
1. Take fresh backup.
2. Restore to `workshop_restore_drill`.
3. Run app against restored DB in non-production slot.
4. Complete smoke checks:
   - login works
   - pricing page loads
   - trial-access route behavior is correct
   - one customer lookup works
5. Record duration and issues in ops log.

## Smoke Test Command
- Run automated flow checks after deployment:
  - `powershell -ExecutionPolicy Bypass -File .\scripts\run-smoke-tests.ps1 -BaseUrl http://workshop.local`
