#!/usr/bin/env bash
set -euo pipefail

# Deliberately overwrite the formerly destructive deployed entrypoint as well.
echo "Name-based candidate cleanup is disabled. Re-run the deployment installer to recover the validated transaction journal." >&2
exit 1
