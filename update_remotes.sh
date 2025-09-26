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

filter_remote() {
	remote_name="$1"
	remote_branch="$2"
	filter_file="$3"
	shift 2
	echo "-- Fetching upstream $remote_name"
	jj git fetch --remote "$remote_name" -b "$remote_branch"
	echo "-- Updating $remote_name subset"
	josh-filter --update refs/heads/${remote_name}-subset-tmp dummy-not-filter --file "$filter_file" refs/remotes/${remote_name}/${remote_branch}
	# ran into issues when I tried to update the branch directly, though I need to re-check
	# it might've been my editor trying to do things at the same time.
	jj bookmark move --allow-backwards ${remote_name}-subset --to ${remote_name}-subset-tmp
	jj bookmark delete ${remote_name}-subset-tmp
}

maybe_create_remote dotnet https://github.com/dotnet/runtime.git
filter_remote dotnet main workflow/dotnet-filter
maybe_create_remote msquic https://github.com/microsoft/msquic.git
filter_remote msquic main workflow/msquic-filter

echo "-- Rebasing onto updated upstreams"
jj rebase -s merge -d workflow -d dotnet-subset -d msquic-subset
