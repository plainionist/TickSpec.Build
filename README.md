
# TickSpec.Build

Generates test classes to execute TickSpec (Gherkin/BDD) based specifications

```bash
TickSpec.Build fixtures FeatureFixtures.fs
```

Automatically integrates into the build process when integrated via NuGet package.


## HTML documentation

Additionally supports generating HTML documents for the feature files

```bash
TickSpec.Build doc . html
```

Use property ``FeatureFileHtmlInput`` to define a different input folder and
property ``FeatureFileHtmlOutput`` to define where the HTML documentation should be generated to.
