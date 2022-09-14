{{/*
Expand the name of the chart.
*/}}
{{- define "operator-api.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "operator-api.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "operator-api.labels" -}}
{{ include "operator-api.selectorLabels" . }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "operator-api.selectorLabels" -}}
app: {{ include "operator-api.name" . }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "operator-api.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "operator-api.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}
