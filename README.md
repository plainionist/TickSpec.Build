
# TickSpec.Build

Generates test classes to execute TickSpec (Gherkin/BDD) based specifications

```bash
TickSpec.Build fixtures FeatureFixtures.fs
```

Automatically integrates into the build process as "BeforeCompile" target when integrated via NuGet package.


## HTML documentation

Additionally supports generating HTML documents for the feature files

```bash
TickSpec.Build doc ./src ./html
```
