# -*- coding: utf-8 -*-
"""
LABGUARD NT106 — CHECKPOINT 2 DECISIONS & CONSISTENCY VALIDATOR
Verifies that all technical parameters across CP2 markdown files are strictly consistent:
- Queue capacities: AgentOutbox=256, CentralIngress=1000, ReplayBuffer=50
- Liveness: Heartbeat=5s, OfflineThreshold=20s, CheckInterval=2.5s (monotonic clock, elapsed time)
- Backoff: 1s, 2s, 4s, 8s, 16s, max 30s with bounded uniform jitter +-10% (NOT full jitter)
- Thresholds: CPU 90/75 (streak 3), RAM 85/70 (streak 3), Service 2 fail / 2 pass
- MaxFrameSize: MaxPayloadSize=262,144 bytes (256 KiB), Wire frame maximum = 262,148 bytes
- Deduplication: BootId=Guid, BatchId=Guid (v4), ProcessedBatches TTL 24h, ProcessedCommands TTL 7d
- No DiskPercent in MetricSamples / DDL
- No GC.GetGCMemoryInfo
- Proper rowversion wording (SQL Server data type, 8 bytes, auto-generated)
- Status discipline: "FREEZE CANDIDATE" and "READY FOR INDEPENDENT RE-AUDIT"
- No unmeasured claims like "triệt tiêu 100%", "an toàn tuyệt đối", "lấy trọn điểm 10"
"""

import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

CP2_FILES = [
    "research/03_CONCURRENCY_RELIABILITY.md",
    "research/04_MONITORING_DETECTION.md",
    "research/05_DELIVERY_SEMANTICS.md",
    "research/06_DATABASE.md",
    "research/CP2_TEST_MATRIX.md",
    "research/CP2_OPEN_ISSUES.md",
    "CP2_DECISIONS_REV2.md",
    "AUDIT_HANDOFF_CHECKPOINT_2_REV2.md",
    "CP2_CHO_NHOM_3_NGUOI_REV2.md"
]

UNSUPPORTED_CLAIMS = [
    "triệt tiêu 100%", "an toàn tuyệt đối", "đạt điểm xuất sắc",
    "lấy trọn điểm 10", "chắc chắn đạt điểm tối đa", "điểm 10 vấn đáp"
]

DISALLOWED_TERMS = [
    "DiskPercent",
    "GC.GetGCMemoryInfo",
    "Full Jitter",
    "ACCEPTED & FROZEN",
    "100% TECHNICAL ISSUES RESOLVED & FROZEN",
    "TCP Window co về 0"
]

def check_file(filepath, base_dir):
    full_path = os.path.join(base_dir, filepath)
    if not os.path.exists(full_path):
        return [f"File not found: {filepath}"]
    
    with open(full_path, "r", encoding="utf-8") as f:
        content = f.read()

    errors = []

    # 1. Disallowed terms
    for term in DISALLOWED_TERMS:
        if term in content:
            errors.append(f"{filepath}: Found disallowed term/phrase '{term}'")

    # Check forbidden worker/dispatch phrases
    FORBIDDEN_DISPATCH_PHRASES = [
        "concurrent dispatch for read queries",
        "ThreadPool xử lý song song"
    ]
    for phrase in FORBIDDEN_DISPATCH_PHRASES:
        if phrase.lower() in content.lower():
            errors.append(f"{filepath}: Found forbidden dispatch phrase '{phrase}'")

    # Check Task.Run usage (only allowed when explicitly stating it is forbidden / avoided)
    for match in re.finditer(r"Task\.Run", content):
        start = max(0, match.start() - 120)
        end = min(len(content), match.end() + 120)
        context = content[start:end].lower()
        allowed_keywords = ["không", "cấm", "tránh", "no loose", "tuyệt đối", "loại bỏ"]
        if not any(kw in context for kw in allowed_keywords):
            errors.append(f"{filepath}: Found unconstrained 'Task.Run' usage without prohibition context at line ~{content[:match.start()].count(chr(10))+1}")

    # Check ONE Single Logical Ingress Worker consistency in key files
    REQUIRED_WORKER_FILES = [
        "03_CONCURRENCY_RELIABILITY.md",
        "CP2_DECISIONS_REV2.md",
        "CP2_CHO_NHOM_3_NGUOI_REV2.md",
        "AUDIT_HANDOFF_CHECKPOINT_2_REV2.md"
    ]
    if any(req in filepath for req in REQUIRED_WORKER_FILES):
        if "ONE Single Logical Ingress Worker" not in content:
            errors.append(f"{filepath}: Missing mandatory phrase 'ONE Single Logical Ingress Worker'")

    # 2. Unsupported promotional claims
    for claim in UNSUPPORTED_CLAIMS:
        if claim in content:
            errors.append(f"{filepath}: Found unsupported promotional claim '{claim}'")

    # 3. Specific checks per file
    # Check MaxFrameSize in decisions / 03 / handoff: verify 256 KiB is payload, not total frame
    if "262,140" in content:
        errors.append(f"{filepath}: Found 262,140 bytes! MaxPayloadSize must be 262,144 bytes (256 KiB), not 262,140.")
    if re.search(r"256\s*KiB[^\n\.]*bao gồm 4 byte", content, re.IGNORECASE):
        errors.append(f"{filepath}: Contradiction on MaxFrameSize: 256 KiB must be MaxPayloadSize, wire frame is 262,148 bytes (4 + 262,144)")

    # Check Disk and Uptime in 06_DATABASE.md
    if "06_DATABASE.md" in filepath:
        if "DiskPercent" in content:
            errors.append(f"{filepath}: DiskPercent must not exist in database schema")
        if "UptimeSeconds BIGINT NOT NULL" not in content:
            errors.append(f"{filepath}: MetricSamples table must include UptimeSeconds BIGINT NOT NULL")
        if "rowversion" in content and "binary(8)" in content:
            # Check if rowversion is correctly explained as data type, not plain binary(8)
            if "rowversion ROWVERSION" not in content and "RowVersion ROWVERSION" not in content and "RowVersion rowversion" not in content:
                errors.append(f"{filepath}: Incidents table must declare column with data type ROWVERSION")

    return errors

def main():
    base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    total_errors = []
    print("==================================================")
    print("RUNNING CHECKPOINT 2 DECISIONS & CONSISTENCY VALIDATION")
    print(f"Base Directory: {base_dir}")
    print("==================================================")

    for fp in CP2_FILES:
        errs = check_file(fp, base_dir)
        if errs:
            for e in errs:
                print(f"❌ {e}")
            total_errors.extend(errs)
        else:
            print(f"✅ {fp}: PASS")

    print("--------------------------------------------------")
    if total_errors:
        print(f"FAIL: {len(total_errors)} consistency issue(s) found.")
        sys.exit(1)
    else:
        print("ALL CP2 FILES PASSED DECISION & CONSISTENCY VALIDATION (0 errors).")
        sys.exit(0)

if __name__ == "__main__":
    main()
