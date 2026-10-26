#!/bin/bash
# ==============================================================================
# CICS Modernization Pipeline Runner Shell Script for Devin IDE
# ==============================================================================
set -e

GROUP="${1:-MG-01}"
PHASE="${2:-all}"

echo "======================================================================"
echo " Starting CICS Modernization Pipeline in Devin IDE"
echo " Transaction Group: $GROUP | Phase: $PHASE"
echo "======================================================================"

if [ "$PHASE" == "all" ]; then
    python3 run_pipeline.py --all --group "$GROUP"
else
    python3 run_pipeline.py --phase "$PHASE" --group "$GROUP"
fi
