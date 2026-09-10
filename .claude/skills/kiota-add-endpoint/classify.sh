#!/usr/bin/env bash
# Tags each scratch file so the caller never has to read generated C#.
# Exits 3 if a shared model lost or retyped a property (needs a human decision).
set -euo pipefail

api="${1:-}"
case "$api" in
    mgmt) real="Descope/Generated/Mgmt" ;;
    auth) real="Descope/Generated/Auth" ;;
    *) echo "usage: classify.sh <mgmt|auth>" >&2; exit 2 ;;
esac

cd "$(git rev-parse --show-toplevel)"
scratch=".kiota-scratch/$api"
if [ ! -d "$scratch" ]; then
    echo "ERROR: no scratch tree at $scratch - run 'make add-$api ENDPOINT=...' first" >&2
    exit 2
fi

# sort -u: kiota emits each property twice, nullable and not
props() { grep -oE 'public [^ ]+ [A-Za-z0-9_]+ \{ get; set; \}' "$1" | awk '{print $3}' | sort -u; }
navs() { grep -oE 'public global::[A-Za-z0-9_.]+RequestBuilder [A-Za-z0-9_]+$' "$1" | awk '{print $NF}' | sort -u; }

# +Name = only in scratch, -Name = only in the real tree
delta() {
    comm -3 <("$1" "$3") <("$1" "$2") | awk -F'\t' 'NF==1 {print "+"$1} NF==2 {print "-"$2}' | tr '\n' ' '
}

blocked=0
while IFS= read -r f; do
    s="$scratch/$f"; r="$real/$f"
    if [ ! -f "$r" ]; then
        echo "NEW      $f"
    elif cmp -s "$s" "$r"; then
        echo "SAME     $f"
    elif [ -n "$(navs "$r")" ]; then   # chain file, incl. the root client
        d=$(delta navs "$r" "$s")
        case "$d" in
            *+*) echo "MERGE    $f   nav: $d" ;;
            *)   echo "PRUNED   $f   (siblings only, leave alone)" ;;
        esac
    else
        d=$(delta props "$r" "$s")
        case "$d" in
            *-*) echo "BLOCKED  $f   props: $d"; blocked=1 ;;
            *+*) echo "ADDITIVE $f   props: $d" ;;
            *)   echo "RESHAPED $f   (no property change; inspect by hand)" ;;
        esac
    fi
done < <(cd "$scratch" && find . -name '*.cs' | sed 's|^\./||' | sort)

if [ "$blocked" -ne 0 ]; then
    echo
    echo "BLOCKED: copying may break other endpoints sharing the model, skipping may break this one. Ask first."
    exit 3
fi
