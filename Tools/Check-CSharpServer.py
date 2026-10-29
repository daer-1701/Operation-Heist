"""Probe the installed DotRush LSP handshake; does not attach to Unity or change code."""
import json
import os
from pathlib import Path
import queue
import shutil
import subprocess
import threading
import time


def main():
    root = Path(__file__).resolve().parent.parent
    extensions = list((Path.home() / ".cursor/extensions").glob("nromanov.dotrush-*"))
    if not extensions:
        raise RuntimeError("DotRush is not installed in Cursor")
    extension = max(extensions, key=lambda path: path.stat().st_mtime)
    server = extension / "extension/bin/LanguageServer/DotRush.dll"
    dotnet = shutil.which("dotnet")
    if not dotnet or not server.is_file():
        raise RuntimeError("Missing .NET or DotRush language server")
    process = subprocess.Popen([dotnet, str(server)], stdin=subprocess.PIPE,
                               stdout=subprocess.PIPE, stderr=subprocess.PIPE, cwd=root)
    messages = queue.Queue()

    def receive():
        try:
            while True:
                header = process.stdout.readline()
                if not header:
                    break
                if not header.lower().startswith(b"content-length:"):
                    continue
                length = int(header.split(b":", 1)[1].strip())
                while process.stdout.readline().strip():
                    pass
                body = bytearray()
                while len(body) < length:
                    chunk = process.stdout.read(length - len(body))
                    if not chunk:
                        raise EOFError("LSP closed early")
                    body.extend(chunk)
                messages.put(json.loads(body))
        except Exception as error:
            messages.put({"probe_error": str(error)})

    def send(payload):
        body = json.dumps(payload).encode("utf-8")
        process.stdin.write(f"Content-Length: {len(body)}\r\n\r\n".encode() + body)
        process.stdin.flush()

    threading.Thread(target=receive, daemon=True).start()
    try:
        send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
            "processId": os.getpid(), "rootUri": root.as_uri(),
            "workspaceFolders": [{"uri": root.as_uri(), "name": "Operation Heist"}],
            "capabilities": {}}})
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline:
            message = messages.get(timeout=max(0.1, deadline - time.monotonic()))
            if "probe_error" in message:
                raise RuntimeError(message["probe_error"])
            if message.get("id") == 1 and "result" in message:
                capabilities = message["result"].get("capabilities", {})
                if not capabilities.get("completionProvider") or not capabilities.get("hoverProvider"):
                    raise RuntimeError("Server did not advertise completion and hover")
                print(json.dumps({"status": "LSP handshake OK", "extension": extension.name,
                                  "completion": True, "hover": True,
                                  "scope": "Server startup only; editor IntelliSense and Unity attach require live verification"}))
                send({"jsonrpc": "2.0", "id": 2, "method": "shutdown", "params": None})
                return
            if "id" in message and "method" in message:
                send({"jsonrpc": "2.0", "id": message["id"], "result": None})
        raise TimeoutError("DotRush initialization timed out")
    finally:
        if process.poll() is None:
            process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()


if __name__ == "__main__":
    main()
