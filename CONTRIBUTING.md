# Contributing

Issues and pull requests are welcome.

- **Bugs:** open an issue with what happened, what you expected, and the smallest code or request that reproduces it.
- **Changes:** for anything beyond a small fix, open an issue first to agree on the approach.

## Making a change

```bash
git clone https://github.com/jmcfadden27/dotnet-service-chassis.git
cd dotnet-service-chassis
dotnet build
dotnet test
```

- Follow the style in `.editorconfig` and the surrounding code.
- Add or update tests for any behaviour you change; `dotnet test` must pass.
- Document new public types and members with XML comments (the build warns when they're missing).
- Update the guide in `docs/` and `CHANGELOG.md` when behaviour or usage changes.
