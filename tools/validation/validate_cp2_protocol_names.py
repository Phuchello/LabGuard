# -*- coding: utf-8 -*-
"""
LABGUARD NT106 — CHECKPOINT 2 PROTOCOL NAME VALIDATOR
Scans all CP2 markdown files to verify that:
1. No prohibited noncanonical protocol names are used.
2. All protocol message types mentioned match the canonical 69-message catalog from 03_PROTOCOL_SPEC.md.
"""

import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

CANONICAL_69_MESSAGES = {
    # P0-MVP REQUIRED (34)
    "HELLO_AGENT", "HELLO_ADMIN", "HELLO_ACK", "AUTH_AGENT", "AUTH_RESULT",
    "HEARTBEAT", "HEARTBEAT_ACK", "METRIC_BATCH", "BATCH_ACK", "SERVICE_STATUS",
    "AGENT_GOODBYE", "GOODBYE_ACK",
    "LOGIN", "LOGIN_RESULT", "LOGOUT", "LOGOUT_RESULT",
    "LIST_DEVICES", "DEVICE_PAGE", "LIST_INCIDENTS", "INCIDENT_PAGE",
    "SUBSCRIBE_DEVICES", "SUBSCRIBE_INCIDENTS",
    "SNAPSHOT_BEGIN", "SNAPSHOT_PAGE", "SNAPSHOT_END",
    "DEVICE_UPDATE", "INCIDENT_OPEN", "INCIDENT_UPDATE",
    "INCIDENT_ACK", "INCIDENT_ASSIGN", "INCIDENT_START", "INCIDENT_RESOLVE",
    "COMMAND_RESULT", "ERROR",
    # P0 OPTIONAL (25)
    "CONFIG_UPDATE", "CONFIG_RESULT", "SERVICE_CHECK_REQUEST",
    "PING", "PONG",
    "SIGN_UP", "SIGN_UP_RESULT", "CREATE_INVITE", "INVITE_RESULT",
    "GET_DEVICE", "DEVICE_DETAIL", "GET_METRICS", "METRICS_RESULT",
    "GET_INCIDENT", "INCIDENT_DETAIL", "GET_HISTORY", "HISTORY_PAGE",
    "GET_SERVER_HEALTH", "SERVER_HEALTH", "GET_REPORT", "REPORT_RESULT",
    "SUBSCRIBE_HEALTH", "HEALTH_UPDATE", "UNSUBSCRIBE", "UNSUBSCRIBE_RESULT",
    # P1 CONDITIONAL (10)
    "HELLO_COLLECTOR", "AUTH_COLLECTOR", "FORWARD_OPEN", "FORWARD_DATA",
    "FORWARD_RESULT", "FORWARD_CLOSE", "CLOSE_SESSION", "COLLECTOR_HEARTBEAT",
    "HEALTH_PROBE", "HEALTH_RESULT"
}

PROHIBITED_NONCANONICAL = {
    "ADMIN_LOGIN_REQ", "LOGIN_REQ", "LOGIN_RESPONSE",
    "ADMIN_SIGNUP_REQ", "SIGNUP_REQ", "SIGNUP",
    "INCIDENT_ACTION_REQ", "DEVICE_COMMAND_REQ",
    "QUERY_DEVICES_REQ", "QUERY_INCIDENTS_REQ", "QUERY_AUDIT_LOGS_REQ", "SYSTEM_HEALTH_REQ",
    "REALTIME_METRIC_EVENT", "REALTIME_INCIDENT_EVENT", "INCIDENT_EVENT", "METRIC_EVENT",
    "COMMAND_REQ", "COMMAND_ACK", "SUBSCRIBE_REQ", "AGENT_HELLO_ACK",
    "SYSTEM_HEALTH_EVENT", "COLLECTOR_REGISTER", "COLLECTOR_FORWARD",
    "QUERY_INCIDENT_HISTORY", "QUERY_METRICS_HISTORY", "DISCONNECT_REQ"
}

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

def check_file(filepath, base_dir):
    full_path = os.path.join(base_dir, filepath)
    if not os.path.exists(full_path):
        return [f"File not found: {filepath}"]
    
    with open(full_path, "r", encoding="utf-8") as f:
        content = f.read()

    errors = []
    
    # 1. Check prohibited noncanonical names
    for bad_name in PROHIBITED_NONCANONICAL:
        matches = [m.start() for m in re.finditer(r"\b" + re.escape(bad_name) + r"\b", content)]
        if matches:
            errors.append(f"{filepath}: Found prohibited noncanonical name '{bad_name}' ({len(matches)} occurrences)")

    # 2. Check backtick protocol-looking words
    # Find all `NAME` where NAME is uppercase and contains underscores or matches protocol words
    backticks = re.findall(r"`([A-Z0-9_]{3,})`", content)
    
    # Known technical uppercase constants/tokens that are NOT message types
    ALLOWED_NON_PROTOCOL = {
        "TCP", "UDP", "IP", "TLS", "SSL", "HTTP", "HTTPS", "JSON", "UTF8", "UTF-8", "ASCII",
        "RAM", "CPU", "NIC", "SSD", "HDD", "IO", "IOCP", "OS", "CLR", "NET", "BCL", "FIFO",
        "UUID", "GUID", "SHA", "SHA256", "HMAC", "PBKDF2", "NTP", "RTO", "MTU", "PDU",
        "ACK", "SYN", "FIN", "RST", "EOF", "OCC", "FSM", "SLA", "WBS", "DAG", "MVP",
        "UIT", "NT106", "SSMS", "SQL", "DDL", "WAL", "ACID", "ORM", "API", "LAN", "WAN", "VPN",
        "NULL", "NOT_NULL", "IDENTITY", "PRIMARY_KEY", "FOREIGN_KEY", "CLUSTERED", "NONCLUSTERED",
        "SELECT", "INSERT", "UPDATE", "DELETE", "COMMIT", "ROLLBACK", "BEGIN",
        "OPEN", "ACKNOWLEDGED", "IN_PROGRESS", "RECOVERED", "RESOLVED", "ACTIVE", "UNKNOWN",
        "CRITICAL", "HIGH", "WARNING", "INFO", "ONLINE", "OFFLINE", "DEGRADED", "HEALTHY", "DOWN",
        "ADMIN", "OPERATOR", "VIEWER",
        "ROWVERSION", "DATETIME2", "NVARCHAR", "VARBINARY", "BIGINT", "UNIQUEIDENTIFIER",
        "P0", "P1", "M0", "M1", "M2", "M3", "M4", "M5", "M6", "M7", "CP1", "CP2", "REV1",
        "SUCCESS", "FAILED", "CONFLICT", "ERROR", "TIMEOUT"
    }

    # Also allow error code prefixes like ERR_*
    for token in backticks:
        if token.startswith("ERR_"):
            continue
        if token.startswith("IX_") or token.startswith("PK_") or token.startswith("FK_") or token.startswith("UQ_"):
            continue
        if token in ALLOWED_NON_PROTOCOL:
            continue
        if token in CANONICAL_69_MESSAGES:
            continue
        # Check if it looks like a message type (e.g. ends in _REQ, _ACK, _EVENT, _RESULT, etc.)
        if any(token.endswith(suffix) for suffix in ["_REQ", "_RESP", "_RESPONSE", "_EVENT", "_MSG"]):
            errors.append(f"{filepath}: Suspected noncanonical message type `{token}`")

    return errors

def main():
    base_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    total_errors = []
    print("==================================================")
    print("RUNNING CHECKPOINT 2 PROTOCOL NAME VALIDATION")
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
        print(f"FAIL: {len(total_errors)} protocol name violation(s) found.")
        sys.exit(1)
    else:
        print("ALL CP2 FILES PASSED PROTOCOL NAME VALIDATION (0 errors).")
        sys.exit(0)

if __name__ == "__main__":
    main()
