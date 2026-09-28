<#
.SYNOPSIS
	Regenerates settings.job from the "WebJob:Schedule" value in appsettings.json.

.DESCRIPTION
	Kudu's built-in WebJob scheduler reads settings.job (not appsettings.json) to determine when
	to trigger a Triggered WebJob. To keep a single, configurable source of truth for the
	schedule, this script is run automatically before build/publish (see Histo.WebJobs.csproj)
	and copies the "WebJob:Schedule" CRON expression from appsettings.json into settings.job.

	Default schedule: "0 0 4 1 1 *" (04:00 UTC on 1 January, every year).
#>
param(
	[Parameter(Mandatory = $true)]
	[string]$AppSettingsPath,

	[Parameter(Mandatory = $true)]
	[string]$SettingsJobPath
)

$ErrorActionPreference = 'Stop'

$config = Get-Content -Raw -Path $AppSettingsPath | ConvertFrom-Json

$schedule = $config.WebJob.Schedule
if ([string]::IsNullOrWhiteSpace($schedule)) {
	throw "WebJob:Schedule is not configured in '$AppSettingsPath'. Add a 'WebJob': { 'Schedule': '0 0 4 1 1 *' } section."
}

$settingsJob = [ordered]@{ schedule = $schedule }
$settingsJob | ConvertTo-Json | Set-Content -Path $SettingsJobPath -Encoding utf8

Write-Host "settings.job updated with schedule '$schedule' (from $AppSettingsPath)"
