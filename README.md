# Dummy.Quic

## Quick Setup Instructions for ensuring `update_remotes.sh` works

Install josh-filter using `cargo install josh-filter --git https://github.com/josh-project/josh.git --rev 68709d4cf3c92d71a3bb796f415db72ecf357d37` (TODO: investigate what's wrong with newer commits)

Install [jujutsu](<https://docs.jj-vcs.dev/latest/install-and-setup/>).

Clone and setup the repo:
```bash
jj git clone https://github.com/kitlith/Dummy.Quic.git
cd Dummy.Quic
jj config set --repo 'revset-aliases."immutable_heads()"' "(present(ooxtuqvy)- ~ workflow::) | present(main@dotnet) | present(main@msquic)"
```

### Usage
```bash
./update_remotes.sh
# or, if you want to pull the latest commits from upstream:
UPDATE=1 ./update_remotes.sh
```
