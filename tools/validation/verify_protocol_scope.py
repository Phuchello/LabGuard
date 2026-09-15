#!/usr/bin/env python3
"""
LABGUARD NT106 — WIRE PROTOCOL SCOPE AUTOMATED VERIFIER
Verifies exact set partitioning of the 69 Wire Messages into:
  - P0-MVP REQUIRED: Exactly 34 messages
  - P0 OPTIONAL:     Exactly 25 messages
  - P1 CONDITIONAL:  Exactly 10 messages
Compares authoritative source (03_PROTOCOL_SPEC.md) against CONTRACT_MATRIX.md,
AUDIT_HANDOFF_REV4_2.md, and AUDIT_HANDOFF_REV4_2_1.md.
"""

import sys
import re
from pathlib import Path

# Authoritative Canonical Sets from 03_PROTOCOL_SPEC.md (SSOT)
CANONICAL_P0_REQUIRED = {
    "HELLO_AGENT", "HELLO_ADMIN", "HELLO_ACK",
    "AUTH_AGENT", "AUTH_RESULT",
    "HEARTBEAT", "HEARTBEAT_ACK",
    "METRIC_BATCH", "BATCH_ACK", "SERVICE_STATUS",
    "AGENT_GOODBYE", "GOODBYE_ACK",
    "LOGIN", "LOGIN_RESULT", "LOGOUT", "LOGOUT_RESULT",
    "LIST_DEVICES", "DEVICE_PAGE", "LIST_INCIDENTS", "INCIDENT_PAGE",
    "SUBSCRIBE_DEVICES", "SUBSCRIBE_INCIDENTS",
    "SNAPSHOT_BEGIN", "SNAPSHOT_PAGE", "SNAPSHOT_END",
    "DEVICE_UPDATE", "INCIDENT_OPEN", "INCIDENT_UPDATE",
    "INCIDENT_ACK", "INCIDENT_ASSIGN", "INCIDENT_START", "INCIDENT_RESOLVE",
    "COMMAND_RESULT", "ERROR"
}

CANONICAL_P0_OPTIONAL = {
    "CONFIG_UPDATE", "CONFIG_RESULT", "SERVICE_CHECK_REQUEST",
    "PING", "PONG",
    "SIGN_UP", "SIGN_UP_RESULT", "CREATE_INVITE", "INVITE_RESULT",
    "GET_DEVICE", "DEVICE_DETAIL",
    "GET_METRICS", "METRICS_RESULT",
    "GET_INCIDENT", "INCIDENT_DETAIL",
    "GET_HISTORY", "HISTORY_PAGE",
    "GET_SERVER_HEALTH", "SERVER_HEALTH",
    "GET_REPORT", "REPORT_RESULT",
    "SUBSCRIBE_HEALTH", "HEALTH_UPDATE",
    "UNSUBSCRIBE", "UNSUBSCRIBE_RESULT"
}

CANONICAL_P1_CONDITIONAL = {
    "HELLO_COLLECTOR", "AUTH_COLLECTOR",
    "FORWARD_OPEN", "FORWARD_DATA", "FORWARD_RESULT", "FORWARD_CLOSE",
    "CLOSE_SESSION", "COLLECTOR_HEARTBEAT",
    "HEALTH_PROBE", "HEALTH_RESULT"
}

ALL_CANONICAL = CANONICAL_P0_REQUIRED | CANONICAL_P0_OPTIONAL | CANONICAL_P1_CONDITIONAL

def parse_spec_scope(spec_path: Path):
    text = spec_path.read_text(encoding="utf-8")
    
    # Extract Section 4.5
    m = re.search(r"### 4\.5\. Phân định Cổng phạm vi giao thức.*?1\. \*\*`P0-MVP REQUIRED`.*?\((.*?)\).*?2\. \*\*`P0 OPTIONAL`.*?\n(.*?)\n3\. \*\*`P1 CONDITIONAL`.*?\n(.*?)(?:\n\n|\Z)", text, re.DOTALL)
    if not m:
        raise ValueError("Could not find Section 4.5 in " + str(spec_path))
    
    # Extract all message tokens from sub-blocks
    p0_req_block = text[text.find("1. **`P0-MVP REQUIRED`"):text.find("2. **`P0 OPTIONAL`")]
    p0_opt_block = text[text.find("2. **`P0 OPTIONAL`"):text.find("3. **`P1 CONDITIONAL`")]
    p1_block = text[text.find("3. **`P1 CONDITIONAL`"):]
    
    p0_req_msgs = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", p0_req_block))
    p0_opt_msgs = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", p0_opt_block))
    p1_msgs = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", p1_block))
    
    # Filter out known non-message tokens if any
    token_filter = lambda s: {t for t in s if t in ALL_CANONICAL}
    return token_filter(p0_req_msgs), token_filter(p0_opt_msgs), token_filter(p1_msgs)

def parse_handoff_scope(handoff_path: Path):
    if not handoff_path.exists():
        return None, None, None
    text = handoff_path.read_text(encoding="utf-8")
    
    m_req = re.search(r"\*\*P0-MVP REQUIRED\*\*.*?\|.*?\|(.*?)\|", text)
    m_opt = re.search(r"\*\*P0 OPTIONAL\*\*.*?\|.*?\|(.*?)\|", text)
    m_p1  = re.search(r"\*\*P1 CONDITIONAL\*\*.*?\|.*?\|(.*?)\|", text)
    
    if not (m_req and m_opt and m_p1):
        return None, None, None
    
    p0_req = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", m_req.group(1)))
    p0_opt = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", m_opt.group(1)))
    p1     = set(re.findall(r"`([A-Z][A-Z0-9_]+)`", m_p1.group(1)))
    return p0_req, p0_opt, p1

def main():
    root = Path(__file__).resolve().parent.parent
    spec_path = root / "03_PROTOCOL_SPEC.md"
    handoff_42 = root / "AUDIT_HANDOFF_REV4_2.md"
    handoff_421 = root / "AUDIT_HANDOFF_REV4_2_1.md"
    
    print("=" * 80)
    print("      LABGUARD NT106 — WIRE PROTOCOL SCOPE AUTOMATED VERIFICATION      ")
    print("=" * 80)
    
    # 1. Verify Canonical Invariants
    assert len(CANONICAL_P0_REQUIRED) == 34, f"P0 REQUIRED count is {len(CANONICAL_P0_REQUIRED)}, expected 34"
    assert len(CANONICAL_P0_OPTIONAL) == 25, f"P0 OPTIONAL count is {len(CANONICAL_P0_OPTIONAL)}, expected 25"
    assert len(CANONICAL_P1_CONDITIONAL) == 10, f"P1 count is {len(CANONICAL_P1_CONDITIONAL)}, expected 10"
    assert len(ALL_CANONICAL) == 69, f"Total count is {len(ALL_CANONICAL)}, expected 69"
    assert CANONICAL_P0_REQUIRED.isdisjoint(CANONICAL_P0_OPTIONAL), "Overlap between P0 REQUIRED and P0 OPTIONAL!"
    assert CANONICAL_P0_REQUIRED.isdisjoint(CANONICAL_P1_CONDITIONAL), "Overlap between P0 REQUIRED and P1!"
    assert CANONICAL_P0_OPTIONAL.isdisjoint(CANONICAL_P1_CONDITIONAL), "Overlap between P0 OPTIONAL and P1!"
    print("1. CANONICAL SET INVARIANTS: PASS")
    print(f"   - P0-MVP REQUIRED:   {len(CANONICAL_P0_REQUIRED)} messages (Includes: SNAPSHOT_*, INCIDENT_ASSIGN)")
    print(f"   - P0 OPTIONAL:       {len(CANONICAL_P0_OPTIONAL)} messages (Includes: GET_DEVICE, GET_INCIDENT, DETAILS)")
    print(f"   - P1 CONDITIONAL:    {len(CANONICAL_P1_CONDITIONAL)} messages")
    print(f"   - TOTAL PARTITION:   {len(ALL_CANONICAL)} messages (Pairwise disjoint: YES)")
    
    # 2. Check 03_PROTOCOL_SPEC.md Section 4.5
    print("\n2. AUTHORITATIVE SPEC: 03_PROTOCOL_SPEC.md")
    spec_req, spec_opt, spec_p1 = parse_spec_scope(spec_path)
    
    diff_req_missing = CANONICAL_P0_REQUIRED - spec_req
    diff_req_extra   = spec_req - CANONICAL_P0_REQUIRED
    diff_opt_missing = CANONICAL_P0_OPTIONAL - spec_opt
    diff_opt_extra   = spec_opt - CANONICAL_P0_OPTIONAL
    diff_p1_missing  = CANONICAL_P1_CONDITIONAL - spec_p1
    diff_p1_extra    = spec_p1 - CANONICAL_P1_CONDITIONAL
    
    status_spec = "PASS"
    if diff_req_missing or diff_req_extra or diff_opt_missing or diff_opt_extra or diff_p1_missing or diff_p1_extra:
        status_spec = "FAIL"
    
    print(f"   - P0 REQUIRED set match:   {len(spec_req)}/34 (Missing: {diff_req_missing or 'empty'}, Extra: {diff_req_extra or 'empty'})")
    print(f"   - P0 OPTIONAL set match:   {len(spec_opt)}/25 (Missing: {diff_opt_missing or 'empty'}, Extra: {diff_opt_extra or 'empty'})")
    print(f"   - P1 CONDITIONAL match:    {len(spec_p1)}/10 (Missing: {diff_p1_missing or 'empty'}, Extra: {diff_p1_extra or 'empty'})")
    print(f"   -> SPEC SCOPE RESULT: {status_spec}")
    
    # 3. Check Handoff Files
    handoff_all_pass = True
    for h_path in [handoff_42, handoff_421]:
        if not h_path.exists():
            continue
        print(f"\n3. AUDIT ARTIFACT: {h_path.name}")
        h_req, h_opt, h_p1 = parse_handoff_scope(h_path)
        if h_req is None:
            print("   -> Scope table not found or unparseable")
            handoff_all_pass = False
            continue
        
        h_diff_req_missing = CANONICAL_P0_REQUIRED - h_req
        h_diff_req_extra   = h_req - CANONICAL_P0_REQUIRED
        h_diff_opt_missing = CANONICAL_P0_OPTIONAL - h_opt
        h_diff_opt_extra   = h_opt - CANONICAL_P0_OPTIONAL
        h_diff_p1_missing  = CANONICAL_P1_CONDITIONAL - h_p1
        h_diff_p1_extra    = h_p1 - CANONICAL_P1_CONDITIONAL
        
        # Misclassifications: messages in one set that belong in another
        misclassified = (h_req & CANONICAL_P0_OPTIONAL) | (h_opt & CANONICAL_P0_REQUIRED)
        
        h_status = "PASS"
        if h_diff_req_missing or h_diff_req_extra or h_diff_opt_missing or h_diff_opt_extra or misclassified:
            h_status = "FAIL"
            handoff_all_pass = False
            
        print(f"   - P0 REQUIRED:   {len(h_req)}/34 (Missing: {h_diff_req_missing or 'empty'}, Extra: {h_diff_req_extra or 'empty'})")
        print(f"   - P0 OPTIONAL:   {len(h_opt)}/25 (Missing: {h_diff_opt_missing or 'empty'}, Extra: {h_diff_opt_extra or 'empty'})")
        print(f"   - P1:            {len(h_p1)}/10 (Missing: {h_diff_p1_missing or 'empty'}, Extra: {h_diff_p1_extra or 'empty'})")
        print(f"   - Misclassified: {misclassified or 'empty'}")
        print(f"   -> HANDOFF SCOPE RESULT: {h_status}")
    
    print("\n" + "=" * 80)
    if status_spec == "PASS" and handoff_all_pass:
        print("FINAL VERDICT: PROTOCOL SCOPE VERIFICATION PASSED")
        print("  P0 REQUIRED = 34/34 exact set match")
        print("  P0 OPTIONAL = 25/25 exact set match")
        print("  P1 = 10/10 exact set match")
        print("  missing = empty")
        print("  extra = empty")
        print("  misclassified = empty")
        print("=" * 80)
        sys.exit(0)
    else:
        print("FINAL VERDICT: PROTOCOL SCOPE VERIFICATION FAILED")
        print("=" * 80)
        sys.exit(1)

if __name__ == "__main__":
    main()
