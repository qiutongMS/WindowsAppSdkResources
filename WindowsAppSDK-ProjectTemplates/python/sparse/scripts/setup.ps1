uv python install 3.12 --install-dir local_python
uv sync --python .\local_python\cpython-3.12.12-windows-x86_64-none\
winapp create-debug-identity .\local_python\cpython-3.12.12-windows-x86_64-none\\python.exe
