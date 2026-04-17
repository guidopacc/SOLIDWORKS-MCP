# 🚀 FIRST WINDOWS TEST GUIDE

**Goal:** Run the first real SolidWorks automation test on your Windows machine

**Time Required:** ~2 hours total (setup + 6 validation checks)

**What You'll Test:** SolidWorks 2022 MCP integration with basic part creation & saving

---

## 📋 Pre-Test Checklist

Before you start, verify you have:

- [ ] Windows 10 or Windows 11
- [ ] SolidWorks 2022 installed & licensed
- [ ] .NET 8 SDK installed
- [ ] This repository cloned on Windows machine
- [ ] PowerShell (admin access preferred)
- [ ] ~1 hour of uninterrupted time
- [ ] Test output directory: `C:\SolidWorksMcpValidation\slice1\`

**Check Everything:**
```powershell
# On Windows, in this repo directory:
.\scripts\validate-windows-environment.ps1
```

If all checks pass ✓ → proceed to Step 1 below

---

## 🔨 Step 1: Build the .NET Worker (15 min)

This compiles the Windows backend that talks to SolidWorks.

```powershell
# In PowerShell (this repo directory)
cd .\adapters\solidworks\dotnet\SolidWorksWorker

dotnet build
```

**Expected Output:**
```
Build succeeded. 0 Warning(s)
```

**If it fails:**
- Check .NET 8 is installed: `dotnet --version`
- Check SolidWorks 2022 is installed
- Check all Visual Studio dependencies
- Reach out with error message

---

## ▶️ Step 2: Start the Worker (5 min)

Keep this running in a terminal during all tests.

```powershell
# Still in SolidWorksWorker directory
dotnet run
```

**Expected Output:**
```
[Worker] Starting SolidWorks MCP Worker...
[Worker] Waiting for handshake request...
```

**IMPORTANT:** Keep this terminal open! Don't close it during testing.

---

## 🧪 Step 3: Run the 6 Validation Checks (60 min)

Open another PowerShell window (keep worker running in first one).

### Check 1: Handshake

**Send this JSON to the worker:**
```json
{"messageType":"handshake_request","requestId":"check-1","protocolVersion":"0.1.0","client":{"name":"manual-validator","version":"0.1.0"},"desiredSolidWorksMajorVersion":2022}
```

**How to send:**
```powershell
# In new PowerShell window, in repo directory
$worker = Start-Process -FilePath dotnet -ArgumentList "run --project .\adapters\solidworks\dotnet\SolidWorksWorker" -NoNewWindow -PassThru

# Then send the JSON via stdin (this gets complex - see full protocol guide)
```

**OR** use the simpler approach - create a test script:

```powershell
# Create file: send-check-1.ps1
$process = Get-Process | Where-Object { $_.ProcessName -eq "dotnet" -and $_.CommandLine -match "SolidWorksWorker" }

if ($process) {
    # Send handshake request
    Write-Host "Sending handshake..."
    # Use named pipes or process stdin
}
```

**Verify Response:**
- `messageType` is `handshake_response`
- `worker.name` is present
- `supportedSolidWorksMajorVersions` includes `2022`

**Document Result:**
```
Check 1 - Handshake: [PASS / FAIL]
Response received: [screenshot or paste]
```

### Check 2: No-Document Failure (5 min)

**Send:**
```json
{"messageType":"execute_command_request","requestId":"check-2","protocolVersion":"0.1.0","command":{"kind":"save_part","path":"C:\\SolidWorksMcpValidation\\slice1\\no-document-test.sldprt"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":[],"currentDocument":null},"desiredSolidWorksMajorVersion":2022}
```

**Verify:**
- Response has `ok: false`
- Error code is `precondition_failed`
- Details show `no_active_document`

**Document:**
```
Check 2 - No-Document Error: [PASS / FAIL]
Error classification: [paste error code]
```

### Check 3: Create New Part (10 min)

**Send:**
```json
{"messageType":"execute_command_request","requestId":"check-3","protocolVersion":"0.1.0","command":{"kind":"new_part","name":"Slice1Validation"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["new_part","save_part"],"currentDocument":null},"desiredSolidWorksMajorVersion":2022}
```

**Verify:**
- Response has `ok: true`
- `nextState.currentDocument.name` is "Slice1Validation"
- `execution.session.hasActiveDocument` is `true`
- File should appear in SolidWorks

**Document:**
```
Check 3 - Create Part: [PASS / FAIL]
Part name in SolidWorks: [screenshot]
State response: [paste relevant fields]
```

### Check 4: Save to Explicit Path (10 min)

**Send:**
```json
{"messageType":"execute_command_request","requestId":"check-4","protocolVersion":"0.1.0","command":{"kind":"save_part","path":"C:\\SolidWorksMcpValidation\\slice1\\slice1-part.sldprt"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["new_part","save_part"],"currentDocument":{"documentType":"part","name":"Slice1Validation","units":"mm","sketches":[],"features":[],"exports":[],"modified":false,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
```

**Verify:**
- Response has `ok: true`
- File exists: `C:\SolidWorksMcpValidation\slice1\slice1-part.sldprt`
- `nextState.currentDocument.savedPath` matches path
- `nextState.currentDocument.modified` is `false`

**Document:**
```
Check 4 - Save to Path: [PASS / FAIL]
File exists at: C:\SolidWorksMcpValidation\slice1\slice1-part.sldprt [YES / NO]
File size: [paste]
Response saveMode: [paste]
```

### Check 5: Save In-Place (5 min)

**Send:** Same path as Check 4:
```json
{"messageType":"execute_command_request","requestId":"check-5","protocolVersion":"0.1.0","command":{"kind":"save_part","path":"C:\\SolidWorksMcpValidation\\slice1\\slice1-part.sldprt"},"stateBefore":{...same state as check-4...},"desiredSolidWorksMajorVersion":2022}
```

**Verify:**
- Response has `ok: true`
- File still exists and is updated
- No path change in response

**Document:**
```
Check 5 - Save In-Place: [PASS / FAIL]
File updated: [YES / NO]
Modified timestamp: [paste]
```

### Check 6: Clean Shutdown (2 min)

**Send:**
```json
{"messageType":"shutdown_request","requestId":"check-6","protocolVersion":"0.1.0"}
```

**Verify:**
- Response is `shutdown_response`
- Worker process exits cleanly
- SolidWorks remains open (if we attached to existing session)

**Document:**
```
Check 6 - Shutdown: [PASS / FAIL]
Worker process exited: [YES / NO]
SolidWorks still running: [YES / NO]
```

---

## 📸 Evidence Collection

For each check, collect:

**Machine Details:**
```
Windows Version: [copy from win+pause or system settings]
SolidWorks Version: [from Help > About]
.NET Version: [from 'dotnet --version']
Timestamp: [when test ran]
```

**Per-Check Evidence:**
- Request JSON sent (copy/paste)
- Response JSON received (copy/paste)
- Any error messages
- Screenshots of SolidWorks state
- File listings (dir C:\SolidWorksMcpValidation\slice1\)

**Example Evidence File:**
```
=== TEST RESULTS ===
Date: 2026-04-15
Machine: GUIDO-PC
Windows: Windows 11 (Build 22621)
SolidWorks: 2022 SP0.0
.NET: 8.0.1

=== CHECK 1: HANDSHAKE ===
Status: PASS
Response:
{
  "messageType": "handshake_response",
  "worker": {
    "name": "SolidWorksWorker",
    "version": "0.1.0"
  },
  ...
}

=== CHECK 2: NO-DOCUMENT ===
Status: PASS
...

[Continue for all 6 checks]
```

---

## 🎯 Success Criteria

All 6 checks should PASS:
- ✅ Handshake successful
- ✅ Error handling works
- ✅ Part creation works
- ✅ Save to path works
- ✅ Save in-place works
- ✅ Clean shutdown

If all ✅ → **Congratulations!** Real SolidWorks integration works!

If some ✗ → Document which ones failed and why → we'll fix it

---

## 🚨 Troubleshooting

### Worker Won't Start
```
Error: Could not find SolidWorks installation
```
- SolidWorks 2022 must be installed and licensed
- Try launching SolidWorks manually once
- Check registry: `HKLM:\SOFTWARE\Wow6432Node\SolidWorks`

### Handshake Fails
```
No response from worker
```
- Check worker is still running in first terminal
- Check firewall isn't blocking stdio
- Try sending simpler JSON (check formatting)

### Part Creation Fails
```
Error: precondition_failed
Details: no_active_document
```
- SolidWorks needs to be running with an active document
- Try creating a part manually first in SolidWorks
- Check COM interop is registered (should be automatic with .NET 8)

### File Not Saved
```
Ok: true, but file doesn't exist
```
- Check path is absolute (C:\, not relative)
- Check directory exists: `mkdir C:\SolidWorksMcpValidation\slice1` first
- Check write permissions

---

## 📝 When Done

**Send Results:**
1. Create file: `TEST_RESULTS_[DATE].md` in this directory
2. Paste all 6 check results + evidence
3. Include any errors or HRESULTs
4. Include suggestions for fixes

**We'll:**
1. Review results
2. Fix any issues found
3. Schedule next test phase

---

## 📖 Reference Documents

If you need more details:
- `docs/architecture/WORKER_PROTOCOL.md` - Full protocol spec
- `docs/architecture/TOOL_CONTRACTS.md` - Tool definitions
- `docs/architecture/SOLIDWORKS_2022_VALIDATION_MATRIX.md` - Detailed validation matrix
- `PROJECT_HANDOFF.md` - Architecture overview

---

**Good luck!** 🚀

This is the moment we validate real Windows integration. Every check that passes confirms the architecture works.

If anything blocks you → document it and we'll troubleshoot together.
