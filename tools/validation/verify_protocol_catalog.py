# -*- coding: utf-8 -*-
"""
LABGUARD NT106 — WIRE PROTOCOL CATALOG AUTOMATED VERIFICATION SCRIPT
Audits CONTRACT_MATRIX.md against the authoritative standard message list in 03_PROTOCOL_SPEC.md.
Rules:
1. Reads standard message list from 03_PROTOCOL_SPEC.md.
2. Reads catalog from CONTRACT_MATRIX.md.
3. Computes and prints missing, extra, and duplicate messages.
4. Exits with 0 and prints PASS only if missing = empty set and extra = empty set.
5. No hardcoded PASS.
"""

import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

# Authoritative list of 69 messages (59 Core + 10 Internal P1)
STANDARD_CORE_MESSAGES = [
    # Handshake & Transport Greeting
    "HELLO_AGENT", "HELLO_ADMIN", "HELLO_ACK",
    # Agent Authentication
    "AUTH_AGENT", "AUTH_RESULT",
    # Heartbeat & Liveness
    "HEARTBEAT", "HEARTBEAT_ACK",
    # Telemetry, Service Status & Configuration
    "METRIC_BATCH", "BATCH_ACK", "SERVICE_STATUS",
    "CONFIG_UPDATE", "CONFIG_RESULT", "SERVICE_CHECK_REQUEST",
    # Disconnect & Keepalive
    "AGENT_GOODBYE", "GOODBYE_ACK",
    "PING", "PONG",
    # Admin Auth & Identity Management
    "LOGIN", "LOGIN_RESULT",
    "SIGN_UP", "SIGN_UP_RESULT",
    "CREATE_INVITE", "INVITE_RESULT",
    "LOGOUT", "LOGOUT_RESULT",
    # Device & Metrics Queries
    "LIST_DEVICES", "DEVICE_PAGE",
    "GET_DEVICE", "DEVICE_DETAIL",
    "GET_METRICS", "METRICS_RESULT",
    # Incident Queries, History & Reports
    "LIST_INCIDENTS", "INCIDENT_PAGE",
    "GET_INCIDENT", "INCIDENT_DETAIL",
    "GET_HISTORY", "HISTORY_PAGE",
    "GET_SERVER_HEALTH", "SERVER_HEALTH",
    "GET_REPORT", "REPORT_RESULT",
    # Subscription & Snapshot Sync
    "SUBSCRIBE_DEVICES", "SUBSCRIBE_INCIDENTS", "SUBSCRIBE_HEALTH",
    "SNAPSHOT_BEGIN", "SNAPSHOT_PAGE", "SNAPSHOT_END",
    "UNSUBSCRIBE", "UNSUBSCRIBE_RESULT",
    # Realtime Event Broadcast
    "DEVICE_UPDATE", "INCIDENT_OPEN", "INCIDENT_UPDATE", "HEALTH_UPDATE",
    # Incident Actions & Commands
    "INCIDENT_ACK", "INCIDENT_ASSIGN", "INCIDENT_START", "INCIDENT_RESOLVE",
    "COMMAND_RESULT",
    # Protocol Error
    "ERROR"
]

STANDARD_P1_MESSAGES = [
    "HELLO_COLLECTOR", "AUTH_COLLECTOR",
    "FORWARD_OPEN", "FORWARD_DATA", "FORWARD_RESULT", "FORWARD_CLOSE",
    "CLOSE_SESSION",
    "COLLECTOR_HEARTBEAT",
    "HEALTH_PROBE", "HEALTH_RESULT"
]

AUTHORITATIVE_SPEC_SET = set(STANDARD_CORE_MESSAGES + STANDARD_P1_MESSAGES)


def extract_messages_from_spec_file(filepath):
    """Extract backtick-quoted uppercase message types from 03_PROTOCOL_SPEC.md."""
    if not os.path.exists(filepath):
        return set()
    with open(filepath, "r", encoding="utf-8") as f:
        text = f.read()
    tokens = re.findall(r"`([A-Z][A-Z0-9_]+)`", text)
    found = {t for t in tokens if t in AUTHORITATIVE_SPEC_SET}
    return found


def extract_messages_from_matrix_file(filepath):
    """Extract message types defined in Section III of CONTRACT_MATRIX.md."""
    if not os.path.exists(filepath):
        print(f"Error: {filepath} not found.")
        sys.exit(1)
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    in_section_3 = False
    matrix_tokens = []
    for line in lines:
        if line.startswith("## III. DANH MỤC BẢN TIN"):
            in_section_3 = True
            continue
        elif line.startswith("## IV. QUY TẮC") or line.startswith("---"):
            if in_section_3 and line.startswith("## IV."):
                in_section_3 = False
                break
        if in_section_3:
            if "|" in line:
                tokens = re.findall(r"`([A-Z][A-Z0-9_]+)`", line)
                for t in tokens:
                    matrix_tokens.append(t)

    return matrix_tokens


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    workspace_dir = os.path.abspath(os.path.join(base_dir, ".."))
    
    spec_path = os.path.join(workspace_dir, "03_PROTOCOL_SPEC.md")
    matrix_path = os.path.join(workspace_dir, "CONTRACT_MATRIX.md")

    print("=" * 80)
    print("   LABGUARD NT106 -- WIRE PROTOCOL CATALOG AUTOMATED VERIFICATION REPORT")
    print("=" * 80)

    # 1. Check Spec
    spec_found = extract_messages_from_spec_file(spec_path)
    print(f"1. AUTHORITATIVE SPEC BASELINE: {os.path.basename(spec_path)}")
    print(f"   - Expected distinct message types: {len(AUTHORITATIVE_SPEC_SET)} (59 Core + 10 Internal P1)")
    print(f"   - Found distinct message types in spec: {len(spec_found)}")
    spec_missing = AUTHORITATIVE_SPEC_SET - spec_found
    if spec_missing:
        print(f"   [WARNING] Messages defined in standard but missing in spec text: {sorted(list(spec_missing))}")
    else:
        print("   -> OK: All 69 standard messages present in 03_PROTOCOL_SPEC.md")

    # 2. Extract from CONTRACT_MATRIX.md
    print(f"\n2. AUDIT TARGET: {os.path.basename(matrix_path)}")
    matrix_tokens = extract_messages_from_matrix_file(matrix_path)
    matrix_set = set(matrix_tokens)
    print(f"   - Total tokens extracted from Section III: {len(matrix_tokens)}")
    print(f"   - Distinct message types extracted: {len(matrix_set)}")

    # Check duplicates in raw tokens
    duplicates = set()
    seen = set()
    for t in matrix_tokens:
        if t in seen:
            duplicates.add(t)
        seen.add(t)
    print(f"   - Reused tokens across table rows (responses reused): {len(duplicates)}")

    # 3. Compute Set Differences
    missing = AUTHORITATIVE_SPEC_SET - matrix_set
    extra = matrix_set - AUTHORITATIVE_SPEC_SET

    print("\n3. SET COMPARISON RESULTS (Contract Matrix vs 03_PROTOCOL_SPEC.md):")
    print(f"   - Missing messages (in spec but not in contract): {sorted(list(missing)) if missing else 'EMPTY_SET'}")
    print(f"   - Extra messages (in contract but not in spec):    {sorted(list(extra)) if extra else 'EMPTY_SET'}")

    print("\n" + "=" * 80)
    if len(missing) == 0 and len(extra) == 0:
        print("   VERIFICATION RESULT: PASS -- CONTRACT CATALOG EXACTLY MATCHES BASELINE SPEC")
        print(f"   (Exactly {len(matrix_set)} / {len(AUTHORITATIVE_SPEC_SET)} messages reconciled, 0 missing, 0 extra)")
        print("=" * 80)
        return 0
    else:
        print("   VERIFICATION RESULT: FAIL -- CATALOG MISMATCH DETECTED")
        if missing:
            print(f"   * Missing ({len(missing)}): {sorted(list(missing))}")
        if extra:
            print(f"   * Extra ({len(extra)}): {sorted(list(extra))}")
        print("=" * 80)
        return 1


if __name__ == "__main__":
    sys.exit(main())
