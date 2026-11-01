# Giant Group Ops Plus (test build)

A separate, full-feature version of the Giant Group field app. The original `GiantGroupMobileApp` is untouched.
It installs **alongside** the original (different app id: `com.thegiantgroup.operationsplus`) and uses the **same server**
(`.../MobileApi.ashx`). **No login anywhere** – open the app and everything is available.

## What's in it
| Area | Features |
|---|---|
| Home | Original dashboard + business tiles (ready to quote, quotes awaiting reply, jobs without crew, outstanding balance) |
| Requests / Jobs / Photos | **Unchanged** from the working app (statuses, measurements, costs, offline photo queue) |
| Calendar | Month agenda of jobs + site evaluations, filters, tap to open |
| Requests (new) | Set site-evaluation date, create a request for a client (GPS pin / address lookup / pasted coordinates), create a quote |
| Quotations | List, filter, search, detail with price breakdown, change status, edit amount, extend expiry, share summary |
| Clients | List, search, add, edit, call/email, their requests, new request for client |
| Allocation | Assign staff / equipment on a job with date + time slots and live clash warnings, remove, allocation overview lists |
| Invoicing & payments | Balance per job, record cash/EFT/card payments, payment history |
| Reports | Money totals, collection %, quote acceptance %, status breakdowns |
| Help & company | FAQ, call / WhatsApp / Instagram / website |
| Settings | Photo queue, Wi-Fi-only uploads, server URL test, **Demo mode** (works offline with sample data for every feature) |

## Setup
1. Apply the website changes first (`../website_changes/README_WEBSITE_CHANGES.txt`) and publish the site.
2. Open `GiantGroupOpsPlus.sln` in Visual Studio 2022 (MAUI workload, .NET 9), build and run on Android.
3. More → Connection → **Test connection**. Tip: switch **Demo mode** on first to look around without the server.
4. APK: Build → Archive/Publish for Android (same as the original app); the application id differs so both can coexist.

## Notes
* Written without access to a compiler: do a full build once and send me any error list – fixes will be quick.
* Photos still queue on the phone first and upload in the background.
* Online (PayFast) payments are recorded by the website automatically; the app records manual payments.
* Not copied from the website because the pages do not exist in the code you sent: Add/Edit Job, Edit Progress, Create Invoice (PDF).
