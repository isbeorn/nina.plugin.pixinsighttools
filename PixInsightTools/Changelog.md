# Changelog

## 1.0.0.1
- Upgrade to .NET7

## 0.1.8.0
- Added an option to "Stack Flats" instruction to stack the flats in the background instead of waiting for them to finish. The live stack loop will then wait for the flats to be stacked first before starting to stack the light frames.
- When adding a color tab you can now specify to only do a color stack for every x number of frames

## 0.1.7.2
- When a target name contains an illegal path character, this is now properly escaped for the stack name to work

## 0.1.7.1
- Added an option to enable ABE by default

## 0.1.7.0
- Quality Gates are now saved and reloaded
- Noise chart scale is now automatically scaled instead of 0..100

## 0.1.6.2
- Added fields to specify calibration pre and postfix

## 0.1.6.1
- Skip RGB Combination if current frame is not relevant

## 0.1.6.0
- ABE degree was always using 4 as value instead of applying the input value
- Added an option to apply SCNR (Green) on color combined stacks

## 0.1.5.1
- Fixed an issue when using BayerPattern = AUTO that the pattern was not passed correctly

## 0.1.5.0
- New feature: Quality gates to ignore images for the stack that do not meet certain critera
- Fixed id numbering for noise evaluation when continuing a previous stack
- Fixed flat calibration leaving behind leftover files

## 0.1.4.0
- Added a multi session FLAT library option, to use FLAT masters across multiple sessions instead of having to set the FLATs each time

## 0.1.3.0
- Added Noise Evaluation scripts and a chart to see Noise Improvements througout the session
- Fixed an issue with debayering parameters