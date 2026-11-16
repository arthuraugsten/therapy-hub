#!/bin/bash
# Runs inside the Keycloak container. Expects KC_ADMIN_PW and DEV_USER_PW in the environment.
set -euo pipefail
kc=/opt/keycloak/bin/kcadm.sh
realm=therapy-hub

$kc config credentials --server http://localhost:8080 --realm master --user admin --password "$KC_ADMIN_PW"

if $kc get "realms/$realm" >/dev/null 2>&1; then
  echo "Realm $realm already exists; nothing to do."
  exit 0
fi

$kc create realms -s realm=$realm -s enabled=true -s displayName="TherapyHub"

# Audience target: the API itself. No login flows; it only appears in the token's "aud".
$kc create clients -r $realm \
  -s clientId=therapy-hub-api -s name="TherapyHub API" -s enabled=true -s publicClient=false \
  -s standardFlowEnabled=false -s directAccessGrantsEnabled=false -s serviceAccountsEnabled=false

# Local development client (public, password grant allowed for testing only).
dev_id=$($kc create clients -r $realm -i \
  -s clientId=therapy-hub-dev -s name="TherapyHub local development" -s enabled=true -s publicClient=true \
  -s standardFlowEnabled=true -s directAccessGrantsEnabled=true \
  -s 'redirectUris=["http://localhost:*","https://localhost:*"]' -s 'webOrigins=["+"]')

$kc create "clients/$dev_id/protocol-mappers/models" -r $realm \
  -s name=therapy-hub-api-audience -s protocol=openid-connect -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=therapy-hub-api' \
  -s 'config."access.token.claim"=true' -s 'config."id.token.claim"=false'

# Fake psychologist for local testing. Not a real person.
$kc create users -r $realm -s username=dev.psychologist -s enabled=true \
  -s email=dev.psychologist@example.com -s emailVerified=true -s firstName=Dev -s lastName=Psychologist
$kc set-password -r $realm --username dev.psychologist --new-password "$DEV_USER_PW"

echo "Realm $realm configured."
