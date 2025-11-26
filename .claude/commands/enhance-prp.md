# Enhance Initial Feature Description

## PRP File: $ARGUMENTS

Enhances PRP file by adding concrete codebase context to prepare it as better input for generate-prp.

## Process

1. **Read PRP file**
  - Understand feature requirements
  - Identify what's missing

2. **Analyze Codebase**
 - Review CLAUDE.md for project overview
 - Find similar patterns in code
 - Identify specific files to reference

3. **Enhance PRP File**
 - Add concrete paths to relevant files
 - Add references to similar implementations in the project
 - Make examples more specific but enrich with context

4. **Backup original PRP file**
 - Backup original prp file using it's original name + suffix "-original'.
 - For example 'INITIAL-FeatureAbc.md' will be backed up to 'INITIAL-FeatureAbc-original.md'

4. **Write Enhanced PRP file**
 - Owervrite original file with enhanced version
 - Preserve original language
 - Add specific file paths and line numbers where relevant

## What to Enhance

## EXAMPLES Section
- Instead of generic references, add concrete paths

## FEATURE Section
- Add notes about existing similar implementations
- Add warnings if there are edge cases in the project

## DOCUMENTATION Section
- Keep external links
- Add internal references to CLAUDE.md and other relevant files

## Output
Overwrites PRP file with enhanced content.
