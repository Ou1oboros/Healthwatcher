# Take-Home Technical Task: Service Monitoring Dashboard

**Candidate:** Musab Alghamdi
**Assigned:** August 18, 2026
**Deadline:** August 25, 2026 (11:59 PM, Riyadh time) — one week
**Estimated effort:** 8–12 hours total (you do not need to spend the whole week on it)

---

## 1. Overview

Build a small full-stack web application that monitors the availability of a set of websites/APIs and displays their status on a live dashboard, then containerize it and deploy it to a local Kubernetes cluster using **minikube**.

The goal of this task is to see how you structure a real project end to end: backend design, frontend implementation, data handling, deployment, and code quality. A small, clean, working app is worth far more than a large, unfinished one.

## 2. Functional Requirements

### Backend (any language/framework you prefer)

1. Monitor **3–5 target URLs** (e.g., `https://google.com`, `https://github.com`, plus any you choose).
   - The list of URLs must be **configurable** (config file, database, or an API endpoint — your choice).
2. Check each URL automatically on a fixed interval (e.g., every 30 seconds) and record for each check:
   - Status (UP / DOWN)
   - HTTP status code
   - Response time in milliseconds
   - Timestamp of the check
3. Persist the check history (SQL Server, SQLite, or in-memory store — your choice, but explain it in the README).
4. Expose a REST API that the frontend uses, including at minimum:
   - Current status of all monitored services
   - Check history for a single service

### Frontend (any framework you prefer)

5. A dashboard page showing all monitored services with:
   - Name / URL
   - Current status (clear visual UP/DOWN indicator)
   - Latest response time
   - Time of last check
6. The dashboard must **update automatically** without a manual page refresh (polling, server-sent events, or WebSockets — your choice).

### Deployment (minikube — required)

7. Containerize both the backend and the frontend (a `Dockerfile` for each).
8. Deploy the full application to a local Kubernetes cluster using **minikube**:
   - Kubernetes manifests (plain YAML, Kustomize, or a Helm chart — your choice) committed to the repository.
   - At minimum: a Deployment and a Service for each component. Use a ConfigMap (or similar) for configuration such as the monitored URL list.
   - The dashboard must be reachable from the host browser (NodePort, Ingress, or `minikube service` — your choice, documented in the README).
9. The README must include the exact commands to go from a fresh minikube cluster to a running app (building images, loading them into minikube, applying manifests).

### Bonus (optional — only if you have time)

- Response-time history chart per service
- Uptime percentage over the last 24 hours
- Ability to add/remove monitored URLs from the UI
- Basic alerting indicator when a service goes down (e.g., banner or badge)
- Kubernetes liveness/readiness probes for your own services
- Horizontal scaling demo (e.g., running the backend with 2+ replicas and explaining what that required)

## 3. Technical Requirements

- **Backend:** any language and framework you prefer — state your choice and why in the README.
- **Frontend:** any framework you prefer — state your choice and why in the README.
- **Deployment:** Docker + Kubernetes manifests, running on minikube.
- Handle failures gracefully: timeouts, unreachable hosts, and invalid URLs must not crash the monitor.
- URL checks should not block each other (consider how you check multiple URLs efficiently).

## 4. Deliverables

Submit a **Git repository** (GitHub — public or private with access shared to us) containing:

1. **Full source code** for backend and frontend.
2. **Dockerfiles and Kubernetes manifests** (or Helm chart) for the minikube deployment.
3. **README.md** with:
   - Your language/framework choices and why
   - Setup and run instructions (assume the reviewer has Docker, minikube, and kubectl installed — exact commands, step by step, from fresh cluster to running app)
   - Brief architecture overview: how the pieces fit together and why you made your main choices (storage, refresh mechanism, project structure)
   - What you would improve or add before running this in production
   - Which bonus items (if any) you implemented
4. **Meaningful commit history** — commit as you work with clear messages. Do not submit everything as a single commit.

Send the repository link to us by the deadline.

## 5. Rules & Notes

- You may use Google, documentation, libraries, and AI tools — the same way you would at work. However, you must **fully understand every part of your submission**.
- After submission, we will hold a **30–45 minute walkthrough session** where you will demo the app, explain your code and decisions, and make a small live modification. Your evaluation depends heavily on this session, not just the code.
- If you cannot finish everything, submit what you have with a note in the README about what is missing and how you would complete it. A working core with honest notes beats a broken "complete" app.
- Questions about the requirements are welcome at any time — asking good clarifying questions is a plus, not a minus.

## 6. Evaluation Criteria

| Criterion | Weight |
|---|---|
| The app runs on minikube by following the README, and core requirements work | 30% |
| Code quality: structure, naming, separation of backend/frontend concerns | 20% |
| Walkthrough session: you can clearly explain and modify your own code | 25% |
| Deployment quality: sensible Dockerfiles, manifests, and configuration | 15% |
| README quality, commit history, and communication | 10% |

Good luck — we look forward to seeing your work.
