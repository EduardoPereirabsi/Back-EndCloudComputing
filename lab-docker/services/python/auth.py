# services/python/auth.py
import os
import requests
from jose import jwt
from fastapi import HTTPException, Depends
from fastapi.security import HTTPBearer

bearer = HTTPBearer(auto_error=False)

# ISSUER = como o token foi emitido (localhost, fora da rede Docker)
# JWKS_URL = onde buscar as chaves (nome interno do container)
ISSUER = os.getenv("OIDC_ISSUER", "http://localhost:8080/realms/lab")
AUD = os.getenv("OIDC_AUDIENCE", "lab-api")
JWKS_URL = os.getenv("OIDC_JWKS", "http://keycloak:8080/realms/lab/protocol/openid-connect/certs")

_jwks_cache = None


def _jwks():
    global _jwks_cache
    if _jwks_cache is None:
        _jwks_cache = requests.get(JWKS_URL, timeout=5).json()
    return _jwks_cache


def require_auth(credentials=Depends(bearer)):
    if not credentials:
        raise HTTPException(status_code=401, detail="missing bearer")
    token = credentials.credentials
    try:
        return jwt.decode(
            token,
            _jwks(),
            options={"verify_aud": True, "verify_iss": True},
            audience=AUD,
            issuer=ISSUER,
        )
    except Exception:
        raise HTTPException(status_code=401, detail="invalid token")
