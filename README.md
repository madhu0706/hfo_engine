This repository contains all files required to deploy the HFO-Engine plug-in for Natus Brain Quick version 4.01 in the Windows environment. Source code may also be utilized to develop unique custom plug-ins for Brain Quick v4.01. To utilize the HFO-Engine plug-in the opensource HFO-Engine server can be downloaded from from https://github.com/shenweiss. This server is designed to operate in the Linux environment.

Software Authors: Shennan Weiss M.D. Ph.D., Tomas Pastore M.S., Matthias Gatti M.S., Federico Raimondo Ph.D., Diego Slezak Ph.D., Madhumathi Devaraj, M.S.

Software Author, Upgrades, Optimization, and Maintenance: Madhumathi Devaraj, M.S.

Upgraded the software from .NET Framework 4.7.1 to .NET Core 6.0 to be compotabile with Brain Quick Version 4.

# HFO Engine Installation Guide

This guide outlines the steps to install and configure the HFO Engine for use with Micromed BrainQuickEEG.

## Prerequisites

* Micromed BrainQuickEEG software installed.
* Git installed.

## Installation Steps

1.  Clone the Repository:
   
    git clone https://github.com/madhu0706/hfo_engine.git

2.  Replace Mock Plugins:
   
    a) Navigate to BrainQuickEEG Plugins directory: `~\Micromed\BrainQuickEEG\Plugins`.
    
    b) Replace the existing mock plugin files with the following files from the cloned repository: `hfo_engine/HFO_ENGINE_Plugins/hfo_plugins`:
    
        * `Micromed.ExternalCalculation.Common.dll`
        * `Micromed.ExternalCalculation.HfoEnginePluginExternalCalculation.dll`
        * `Micromed.ExternalCalculation.HfoEnginePluginExternalCalculation.deps.json`
        * `Micromed.ExternalCalculation.HfoEnginePluginExternalCalculation.pdb`

4.  Create the `hfo_engine` Directory:
    * In the `~\Micromed\BrainQuickEEG\Plugins` directory, create a new folder named `hfo_engine`.

5.  Copy HFO Engine Binaries:
    * Copy all folders and files from the following directory in the cloned repository: `hfo_engine/UI/HFO_ENGINE/bin/Release/net6.0-windows/win-x64`
    * Paste these files into the `~\Micromed\BrainQuickEEG\Plugins\hfo_engine` directory.

6.  Create `temp` and `logs` Folders (if necessary):
    * Inside the `~\Micromed\BrainQuickEEG\Plugins\hfo_engine` directory, create two new folders named `temp` and `logs` if they do not already exist. These folders will store temporary trace files and log files, respectively.

## Usage Instructions

1.  Open External Calculation:
    * Launch the BrainQuickEEG agent.
    * Open the "External calculation" feature.
    * Select "HFO Engine calculation" from the dropdown menu.
      
2.  Configure Trace File Path:
    * The trace file path should be automatically populated.
    * Click "Save" to confirm.
      
3.  Select Montages and Time Interval:
    * Choose the desired montages for analysis.
    * Specify the time interval for processing.
      
4.  Start Analysis:
    * Click "Start" to begin the HFO detection process.
    * The processed event file will be saved to `C:\system98\temp`.

5.  Completion Message:
    * Once the analysis is complete, a message "Analysis Completed. Please close the HFO_ENGINE." will appear.
      
6.  Close HFO Engine:
    * Close the HFO_ENGINE application.
    * Wait for the calculation to finish.
    * Click "OK" in the BrainQuickEEG agent.
      
7.  View Results:
    * The event file is created.
    * Annotations are automatically populated in the BrainQuickEEG agent.

## OUTCOME:

This upgrade aligns the HFO Engine plugin with enterprise-grade engineering practices. The migration accommodates .NET 6.0 architecture, CLI-based control, rich GUI compatibility, and single-file deployment for clinical EEG workflows. The project is now fully positioned for scalable, secure, and modern EEG analysis deployments across Micromed-supported environments.These changes collectively bring the HFO Engine application in line with .NET 6.0 best practices, modernize its architecture for plugin-based deployment in Brain Quick 4.x, and ensure compatibility with future-facing EEG analysis workflows. Upgraded the target framework from legacy .NET Framework to net6.0-windows, enabling cross-version support and forward compatibility. Explicit UI stack support for both WinForms and WPF retained (<UseWindowsForms> and <UseWPF>), ensuring future extensibility toward hybrid UIs if needed. Ensures compatibility with Windows APIs while using lightweight, modern libraries. 

