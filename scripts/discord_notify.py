#!/usr/bin/env python3
"""discord_notify.py — 하네스 → 디스코드 알림 (S-018).

웹훅 URL은 커밋 금지(비밀값) — 각 PC의 `git config dontlate.webhook`에서 읽는다.
미설정이면 조용히 종료(알림은 부산물, 기록의 정본은 git 대장).

사용:
  python scripts/discord_notify.py "메시지"
  python scripts/discord_notify.py "스크린샷" --file Screenshots/foo.png
"""
import json
import subprocess
import sys
import urllib.request
import uuid
from pathlib import Path


def webhook_url() -> str | None:
    try:
        out = subprocess.run(
            ["git", "config", "dontlate.webhook"],
            capture_output=True, text=True, timeout=5)
        url = out.stdout.strip()
        return url if url.startswith("https://discord.com/api/webhooks/") else None
    except Exception:
        return None


# ⚠ User-Agent 필수 — 파이썬 기본 UA는 디스코드(Cloudflare)가 차단한다 (실측 403, 2026-07-22).
UA = "DontLate-Harness/1.0"


def post_text(url: str, content: str) -> None:
    body = json.dumps({"content": content[:1900]}).encode("utf-8")  # 디스코드 2000자 상한
    req = urllib.request.Request(
        url, data=body,
        headers={"Content-Type": "application/json", "User-Agent": UA})
    urllib.request.urlopen(req, timeout=10).read()


def post_file(url: str, content: str, path: Path) -> None:
    boundary = uuid.uuid4().hex
    payload = json.dumps({"content": content[:1900]})
    data = (
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"payload_json\"\r\n\r\n{payload}\r\n"
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"files[0]\"; filename=\"{path.name}\"\r\n"
        f"Content-Type: application/octet-stream\r\n\r\n"
    ).encode("utf-8") + path.read_bytes() + f"\r\n--{boundary}--\r\n".encode("utf-8")
    req = urllib.request.Request(
        url, data=data,
        headers={"Content-Type": f"multipart/form-data; boundary={boundary}", "User-Agent": UA})
    urllib.request.urlopen(req, timeout=30).read()


def main() -> int:
    args = sys.argv[1:]
    if not args:
        return 0
    url = webhook_url()
    if url is None:
        return 0  # 미설정 = 알림 생략 (실패 아님)

    file_path: Path | None = None
    if "--file" in args:
        i = args.index("--file")
        file_path = Path(args[i + 1])
        args = args[:i] + args[i + 2:]
    message = " ".join(args)

    try:
        if file_path is not None and file_path.exists():
            post_file(url, message, file_path)
        else:
            post_text(url, message)
    except Exception:
        return 0  # 알림 실패가 공정을 멈추면 안 된다
    return 0


if __name__ == "__main__":
    sys.exit(main())
