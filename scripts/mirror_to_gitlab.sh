#!/usr/bin/env bash
# Cree un repo prive sur GitLab et y mirror le repo GitHub DonTroc.
# Prerequis : glab auth login (faire une fois manuellement)
set -e

REPO_NAME="DonTroc"
VISIBILITY="private"  # private | internal | public
DESCRIPTION="DonTroc - Don & Troc d'Objets (mirror prive de github.com/bachirtech/DonTroc)"

cd "$(dirname "$0")/.."
ROOT="$(pwd)"
echo "Repo local : $ROOT"

# 1) Verifier auth
if ! glab auth status >/dev/null 2>&1; then
  echo "ERREUR : glab non authentifie."
  echo "Lance d'abord : glab auth login"
  exit 1
fi

GITLAB_USER="$(glab api user --jq .username)"
echo "GitLab user : $GITLAB_USER"

# 2) Creer le repo (idempotent : continue si existe deja)
echo "Creation du projet GitLab $GITLAB_USER/$REPO_NAME..."
glab repo create "$REPO_NAME" \
  --"$VISIBILITY" \
  --description "$DESCRIPTION" \
  --defaultBranch main 2>&1 || echo "(deja existant, on continue)"

# 3) Ajouter remote gitlab si absent
GITLAB_URL="git@gitlab.com:$GITLAB_USER/$REPO_NAME.git"
if git remote get-url gitlab >/dev/null 2>&1; then
  git remote set-url gitlab "$GITLAB_URL"
else
  git remote add gitlab "$GITLAB_URL"
fi
echo "Remote gitlab -> $GITLAB_URL"

# 4) Push toutes les branches + tags (mirror)
echo "Push --mirror vers GitLab..."
git push --mirror gitlab

echo ""
echo "OK ! Repo mirror disponible sur :"
echo "  https://gitlab.com/$GITLAB_USER/$REPO_NAME"
echo ""
echo "Pour synchroniser apres chaque push GitHub :"
echo "  git push gitlab main"
echo ""
echo "Ou pour automatiser via une git alias :"
echo "  git config alias.pushall '!git push origin main && git push gitlab main'"
echo "  git pushall"

