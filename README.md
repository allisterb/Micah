# Micah

# Problem statement - EHR pain points
* Too much time spent fighting EHR systems vs. spending time with patients
* Complex, unforgiving interface actions required for querying, searching, retrieving information from EHR
* Mode mismatch entering and querying patient data vs. conversing with patient or other staff
* Software or interface changes or additions require retraining
* Physician burnout... 
* Proprietary software promotes vendor lock-in, patient data silos
* Cost

-----
# STOP! - How to avoid medical 'Clippy'?
* Use deterministic rule-based dialogue manager based on NLU semantic parsing
* NLU technology is maturing rapidly
* Models trained on knowledge graphs, not simple conversations
* In the lab ASR approaching human-level recognition
* It's going to happen....
-----
# Typical use case
1. User logs in to Micah over the Web
2. Authentication via OAuth and optional biometric authentication
3. User selects public-facing FHIR server or uses Google Healthcare store
4. User queries patient records FHIR store using natural language e.g 
    * `Find Michael Parks from Manhattan`
    * `show me all vital observations for Michael Parks since last Thursday`
5. Single query can potentially be sent to multiple FHIR servers
6. User adds notes, observations, Health & Physical Examination....
7. Micah extracts FHIR resources from text and stores it in designated server

-----
# Implementation - Overview
* Written in F# and runs on .NET and PGSQL
* Hosted on RedHat OpenShift Kubernetes-based PaaS
* NLU services:
    * Wit.ai
    * Google Healthcare NLU
    * expert.ai
* Firely .NET FHIR libraries
* Connects to Google Healthcare FHIR store + public FHIR stores
* Can use facial and TypingDNA typing biometric authentication

-----
# Implementation - Security
* OAuth authentication using Google
* In-browser biometric facial recognition e.g via. Azure Face recognition 
* In-browser biometric typing recognition via TypingDNA
* Google Healthcare is HIPAA compliant
* Google Healthcare FHIR uses OAuth authentication
