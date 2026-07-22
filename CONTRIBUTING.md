# Rules for Contributing

Ktisis is an open-source project under GPL v3.0 and is open to contribution from any authors. The following rules must be observed by all prospective contributors and maintainers:

1. All contributors must adhere to the [Dalamud Code of Conduct](https://github.com/goatcorp/governance/blob/main/code-of-conduct.md).
2. Contributions must be made via Pull Request, to be publicly reviewed & approved by a Ktisis maintainer. Exceptions may be made for small-scale maintenance or emergency work.
3. Any usage of generative AI must be disclosed in a Pull Request. Per disclosure levels in the [Dalamud AI Policy](https://github.com/goatcorp/governance/blob/main/ai-policy-official-repo.md), the only acceptable levels of AI usage in this project are **None** and **Hint** (auto-correct/auto-suggest).
4. **No code involving generative AI**, used at any point in its lifecycle, will be accepted.
5. Reviewers will, to the best of their ability, evaluate submitted code for effectiveness, best practice, and signs of generative AI usage. Suspect commits or documentation may be interrogated non-judgmentally - understand that by submitting to this repository, your Git history may be evaluated in the course of the review process.
6. Failure to adhere to any of the above restrictions will be met with appropriate consequences per the Code of Conduct. Punishments range from **Warning** to **Permanent Ban** depending on severity, and will be publicly disclosed to the community where applicable.

These rules were authored to align with community sentiment, to improve accountability & transparency on the part of maintainers, and to ensure that Ktisis remains a healthy & human-made tool.

# Best Practices

Before opening a pull request, we recommend that contributors:
- Limit all changes to manipulation of objects solely inside of GPose
- Do not automate any tasks which communicate network packets to the server
- Ensure that any changes to the client state are not irreversible by the user or detectable by the server
- Familiarize yourself with existing style & design conventions throughout the codebase
- Take great care when writing unsafe code, or avoid it entirely if unfamiliar with the risks
- Review existing documentation at [docs.ktisis.tools](https://docs.ktisis.tools/) and consider drafting an additional PR to [docs](https://github.com/ktisis-tools/docs/) if your change would benefit from it
- Reach out to developers in the [community discord](https://discord.ktisis.tools/) with any questions
