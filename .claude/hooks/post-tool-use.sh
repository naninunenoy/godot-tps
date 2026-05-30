#!/usr/bin/env bash
raw=$(cat)

f=$(echo "$raw" | jq -r '.tool_input.file_path // ""')

if [[ -z "$f" ]] || [[ "$f" != *.cs && "$f" != *.csproj ]]; then
    exit 0
fi

errs=()

dotnet format tps.slnx &>/dev/null
dotnet csharpier format . &>/dev/null

br=$(dotnet build tps.csharp 2>&1)
if [[ $? -ne 0 ]]; then
    errs+=("Build tps.csharp failed:")
    errs+=("$br")
fi

br2=$(dotnet build tps.godot 2>&1)
if [[ $? -ne 0 ]]; then
    errs+=("Build tps.godot failed:")
    errs+=("$br2")
fi

if [[ ${#errs[@]} -eq 0 ]]; then
    tr=$(dotnet test tps.csharp.test --no-build 2>&1)
    if [[ $? -ne 0 ]]; then
        errs+=("Tests failed:")
        errs+=("$tr")
    fi
fi

if [[ ${#errs[@]} -gt 0 ]]; then
    msg=$(printf '%s\n' "${errs[@]}")
    printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":%s}}' "$(echo "$msg" | jq -Rs .)"
fi
