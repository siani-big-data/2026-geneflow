"""
End-to-end validation for the GeneFlow analysis pipeline (stdlib-only).

Run:
  python e2e_analysis.py
"""

from __futuREDACTED import annotations

import json
import mimetypes
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path


def verify_email_in_db(email: str) -> None:
    """Manually flip email_verified=true for E2E so we don't depend on Mailpit."""
    sql = (f"UPDATE identity.users SET email_verified = true "
           f"WHERE email = '{email}';")
    res = subprocess.run(
        ["docker", "exec", "geneflow-core-postgres",
         "psql", "-U", "geneflow", "-d", "geneflow", "-c", sql],
        captuREDACTED=True, text=True, timeout=20,
    )
    print(f"    db verify: {res.stdout.strip() or res.stderr.strip()}")

API = "http://localhost:5145"
AB1_PATH = Path(r"C:\develop\geneflow\geneflow-ai\datalake\ab1\11_27F.ab1")
PROC_TIMEOUT = 90
RESULT_TIMEOUT = 180

ANALYSIS_REQUESTS = {
    "trimming": {"algorithm": "modified_mott", "qualityThreshold": 20, "windowSize": 10},
    "heterozygote": {"secondaryPeakThreshold": 0.3, "minQuality": 20},
    "motif": {"pattern": "ATG", "searchComplement": False, "useRegex": False},
    "translation": {"frame": 1, "geneticCode": 1},
    "orf": {"minLength": 30, "startCodons": "ATG", "stopCodons": "TAA,TAG,TGA"},
    "restriction": {"enzymes": "EcoRI,BamHI,HindIII"},
}


def fail(msg: str) -> None:
    print(f"FAIL: {msg}")
    sys.exit(1)


def request(method: str, url: str, headers=None, body_bytes: bytes | None = None,
            content_type: str | None = None, timeout: int = 30):
    h = dict(headers or {})
    if content_type:
        h["Content-Type"] = content_type
    req = urllib.request.Request(url, data=body_bytes, method=method, headers=h)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return resp.getcode(), resp.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        try:
            data = e.read().decode("utf-8", errors="replace")
        except Exception:
            data = ""
        return e.code, data
    except urllib.error.URLError as e:
        return -1, str(e)


def post_json(url, payload, headers=None, timeout=30):
    body = json.dumps(payload).encode("utf-8")
    return request("POST", url, headers=headers, body_bytes=body,
                   content_type="application/json", timeout=timeout)


def get_json(url, headers=None, timeout=30):
    return request("GET", url, headers=headers, timeout=timeout)


def post_multipart(url, headers, fields: dict, file_field: str, file_path: Path):
    boundary = f"----geneflowE2E{uuid.uuid4().hex}"
    crlf = "\r\n"
    parts = []
    for k, v in fields.items():
        parts.append(f"--{boundary}{crlf}"
                     f'Content-Disposition: form-data; name="{k}"{crlf}{crlf}'
                     f"{v}{crlf}")
    file_bytes = file_path.read_bytes()
    mime = mimetypes.guess_type(str(file_path))[0] or "application/octet-stream"
    head = (f"--{boundary}{crlf}"
            f'Content-Disposition: form-data; name="{file_field}"; '
            f'filename="{file_path.name}"{crlf}'
            f"Content-Type: {mime}{crlf}{crlf}")
    tail = f"{crlf}--{boundary}--{crlf}"

    body = ("".join(parts) + head).encode("utf-8") + file_bytes + tail.encode("utf-8")
    ct = f"multipart/form-data; boundary={boundary}"
    return request("POST", url, headers=headers, body_bytes=body,
                   content_type=ct, timeout=120)


def main() -> None:
    if not AB1_PATH.exists():
        fail(f"Trace file not found: {AB1_PATH}")

    suffix = uuid.uuid4().hex[:8]
    email = f"e2e_{suffix}@example.com"
    username = f"e2e{suffix}"
    password = "Sup3rSecret_e2e!"

    print(f"[1] Register {email}")
    code, body = post_json(f"{API}/api/v1/auth/register",
                           {"email": email, "username": username, "password": password})
    print(f"    -> {code}")
    if code not in (200, 201):
        print(f"    body: {body[:400]}")
        fail("register failed")

    print("[1b] Mark email as verified directly in DB (E2E shortcut)")
    verify_email_in_db(email)

    print("[2] Login")
    code, body = post_json(f"{API}/api/v1/auth/login",
                           {"identifier": email, "password": password})
    print(f"    -> {code}")
    if code != 200:
        print(f"    body: {body[:400]}")
        fail("login failed")
    j = json.loads(body)
    token = (j.get("accessToken")
             or j.get("token")
             or j.get("jwt")
             or (j.get("tokens") or {}).get("accessToken"))
    if not token:
        fail(f"no token in: {body[:300]}")
    headers = {"Authorization": f"Bearer {token}"}

    print("[3] Create study")
    code, body = post_json(
        f"{API}/api/v1/studies/",
        {"title": f"E2E Analysis Study {suffix}",
         "description": "Automated E2E validation",
         "researchFieldId": 1},
        headers=headers,
    )
    print(f"    -> {code}")
    if code not in (200, 201):
        print(f"    body: {body[:400]}")
        fail("create study failed")
    j = json.loads(body)
    study_id = j.get("id") or j.get("studyId")
    if not study_id:
        fail(f"no study id: {body[:300]}")
    print(f"    studyId = {study_id}")

    print(f"[4] Upload trace {AB1_PATH.name}")
    code, body = post_multipart(
        f"{API}/api/v1/studies/{study_id}/traces/upload",
        headers,
        {"name": f"E2E_{suffix}", "description": "E2E"},
        "file",
        AB1_PATH,
    )
    print(f"    -> {code}")
    if code not in (200, 201):
        print(f"    body: {body[:500]}")
        fail("upload failed")
    j = json.loads(body)
    trace_id = j.get("id") or j.get("traceId")
    if not trace_id:
        fail(f"no trace id: {body[:300]}")
    print(f"    traceId = {trace_id}")

    print("[5] Wait for processing")
    deadline = time.time() + PROC_TIMEOUT
    last = None
    while time.time() < deadline:
        code, body = get_json(
            f"{API}/api/v1/studies/{study_id}/traces/{trace_id}", headers)
        if code == 200:
            try:
                j = json.loads(body)
            except Exception:
                j = {}
            status = j.get("status") or j.get("processingStatus")
            if status != last:
                print(f"    status = {status}")
                last = status
            sl = str(status).lower()
            if sl in ("processed", "completed", "ready"):
                break
            if sl in ("failed", "error"):
                fail(f"processing failed: {body[:400]}")
        time.sleep(2)
    else:
        fail(f"trace did not finish in {PROC_TIMEOUT}s, last={last}")

    print("[6] Trigger analyses")
    triggered = {}
    for atype, payload in ANALYSIS_REQUESTS.items():
        code, body = post_json(
            f"{API}/api/v1/traces/{trace_id}/analysis/{atype}",
            payload, headers=headers)
        triggered[atype] = code
        print(f"    POST {atype:14s} -> {code}")
        if code not in (200, 202):
            print(f"      body: {body[:300]}")

    print("[7] Poll list endpoint")
    expected = set(ANALYSIS_REQUESTS.keys())
    seen = set()
    deadline = time.time() + RESULT_TIMEOUT
    while time.time() < deadline:
        code, body = get_json(
            f"{API}/api/v1/traces/{trace_id}/analysis/", headers)
        if code == 200:
            try:
                data = json.loads(body)
                if isinstance(data, list):
                    types = data
                elif isinstance(data, dict):
                    types = (data.get("availableTypes")
                             or data.get("analysisTypes")
                             or data.get("types")
                             or [])
                else:
                    types = []
                seen = {str(t).lower() for t in types}
                missing = sorted(expected - seen)
                print(f"    seen={sorted(seen)} missing={missing}")
            except Exception as ex:
                print(f"    parse error: {ex}; raw={body[:200]}")
        else:
            print(f"    LIST -> {code} {body[:200]}")
        if expected.issubset(seen):
            break
        time.sleep(3)

    print("[8] GET each analysis payload")
    results = {}
    for atype in ANALYSIS_REQUESTS:
        code, body = get_json(
            f"{API}/api/v1/traces/{trace_id}/analysis/{atype}", headers)
        ok = code == 200
        preview = body[:160].replace("\n", " ")
        results[atype] = (code, ok, preview)
        print(f"    GET {atype:14s} -> {code}  {preview}")

    print("\n=== SUMMARY ===")
    print(f"trace status        : {last}")
    print(f"trigger codes       : {triggered}")
    print(f"types in list       : {sorted(seen)}")
    print(f"types still missing : {sorted(expected - seen)}")
    print("per-type GET status :")
    for atype, (code, ok, _) in results.items():
        marker = "OK " if ok else "FAIL"
        print(f"  [{marker}] {atype:14s} HTTP {code}")

    failures = [t for t, (_c, ok, _p) in results.items() if not ok]
    missing = sorted(expected - seen)
    if failures or missing:
        print("\nRESULT: PARTIAL / FAIL")
        sys.exit(2)
    print("\nRESULT: ALL ANALYSIS TYPES OK")


if __name__ == "__main__":
    main()
