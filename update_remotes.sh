#!/usr/bin/env bash

maybe_create_remote() {
	local remote_name="$1"
	local remote_url="$2"

	if jj git remote list | cut -d ' ' -f 1 | grep -q "$remote_name"; then
		: # remote already exists, do nothing
	else
		echo "-- Adding upstream remote '$remote_url' for remote '$remote_name'"
		jj git remote add "$remote_name" "$remote_url"
	fi

}

filter_remote() {
	local remote_name="$1"
	local remote_branch="$2"
	local filter_file="$3"
	echo "-- Fetching upstream $remote_name"
	jj git fetch --remote "$remote_name" -b "$remote_branch"
	echo "-- Updating $remote_name subset"
	josh-filter --update refs/heads/${remote_name}-subset dummy-not-filter --file "$filter_file" refs/remotes/${remote_name}/${remote_branch}
}

maybe_create_remote dotnet https://github.com/dotnet/runtime.git
filter_remote dotnet main workflow/dotnet-filter
maybe_create_remote msquic https://github.com/microsoft/msquic.git
filter_remote msquic main workflow/msquic-filter

echo "-- Rebasing onto updated upstreams"
abandon_parents=$(jj log --no-graph -T 'change_id ++ " | "' -r 'merge- ~ workflow::')
workflow_parent=$(jj log --no-graph -T 'change_id ++ " | "' -r 'merge- & workflow::')
jj rebase -s merge -d "$workflow_parent none()" -d dotnet-subset -d msquic-subset

echo "-- Abandoning old versions of upstreams"
jj abandon -r "..($abandon_parents none()) ~ ..merge"
