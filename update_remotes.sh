#!/usr/bin/env bash

maybe_create_remote() {
	remote_name="$1"
	remote_url="$2"

	if jj git remote list | cut -d ' ' -f 1 | grep -q "$remote_name"; then
		: # remote already exists, do nothing
	else
		echo "-- Adding upstream remote '$remote_url' for remote '$remote_name'"
		jj git remote add "$remote_name" "$remote_url"
	fi

}

maybe_create_remote dotnet https://github.com/dotnet/runtime.git
echo "-- Fetching upstream dotnet"
jj git fetch --remote dotnet -b main
echo "-- Updating dotnet subset"
josh-filter --file workflow/dotnet-filter not-a-filter --update refs/heads/dotnet-subset-tmp refs/remotes/dotnet/main
jj bookmark move dotnet-subset --to dotnet-subset-tmp --allow-backwards
jj bookmark delete dotnet-subset-tmp

maybe_create_remote msquic https://github.com/microsoft/msquic.git
echo "-- Fetching upstream msquic"
jj git fetch --remote msquic -b main
echo "-- Updating msquic interop bindings"
josh-filter ':[src/Interop=:/src/cs/lib]' --update refs/heads/msquic-subset-tmp refs/remotes/msquic/main
jj bookmark move msquic-subset --to msquic-subset-tmp --allow-backwards
jj bookmark delete msquic-subset-tmp

echo "-- Rebasing onto updated upstreams"
jj rebase -s merge -d workflow -d dotnet-subset -d msquic-subset
