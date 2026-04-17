# RECEIVER DRY-RUN CHECKLIST

## Purpose

Define what "receiver dry-run" means for the current repo-first alpha handoff and how to judge whether the package is actually usable by a technical receiver.

## Scope

This checklist validates:

- package comprehension
- entry-document clarity
- file discoverability
- distinction between repo, companion package, and evidence pack
- setup and connection understanding without project-memory shortcuts

It does not validate:

- new CAD capability
- broader SolidWorks version support
- installer-grade distribution
- desktop-extension packaging

## Allowed assumptions

### Sender-side assumptions

These are allowed:

- the receiver has the repository source checkout
- the receiver has the alpha handoff companion package
- the receiver may also have an optional evidence pack
- the receiver is a technical developer on Windows

These are not allowed:

- assuming the receiver knows the historical project path
- assuming the receiver knows which docs are package-copied versus repo-only unless the docs say so
- assuming the receiver knows whether to rebuild the worker or use the included worker copy unless the docs say so
- assuming the receiver knows unsupported tools from prior project history

## Receiver actual steps

The receiver dry-run should follow this minimum path:

1. open the alpha handoff companion package
2. read `alpha-handoff-summary.txt`
3. follow the recommended read order
4. determine what the package is and is not
5. determine what still requires the repository checkout
6. locate config examples
7. understand how the worker path is expected to work
8. determine the first safe connection and modeling checks

## Success criteria

The dry-run is successful when all of the following are true:

1. the receiver can explain the difference between:
   - repository source
   - alpha handoff companion package
   - optional evidence pack
2. the receiver can find the key docs without project-memory shortcuts
3. the receiver can identify the current public alpha boundary honestly
4. the receiver can identify the intended client setup path
5. the receiver can understand whether to rebuild the worker or use the included worker copy
6. the receiver can identify the first safe post-connection checks
7. no remaining friction blocks a technical receiver from orienting and attempting setup

## Evidence to capture

- package path tested
- read order actually followed
- files that were easy to find
- files or steps that were ambiguous
- any sender-centric wording that remained
- any fix applied after the first pass
- final yes/no judgment for `receiver-validated`
