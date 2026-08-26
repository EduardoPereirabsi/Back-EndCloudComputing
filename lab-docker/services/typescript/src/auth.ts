// services/typescript/src/auth.ts
import { createRemoteJWKSet, jwtVerify } from "jose";
import type { Request, Response, NextFunction } from "express";

// issuer = como o token foi emitido (localhost, fora da rede Docker)
// jwks   = onde buscar as chaves (nome interno do container)
const issuer = process.env.OIDC_ISSUER || "http://localhost:8080/realms/lab";
const audience = process.env.OIDC_AUDIENCE || "lab-api";
const jwksUri = process.env.OIDC_JWKS || "http://keycloak:8080/realms/lab/protocol/openid-connect/certs";

const jwks = createRemoteJWKSet(new URL(jwksUri));

export async function requireAuth(req: Request, res: Response, next: NextFunction) {
  try {
    const auth = req.headers.authorization || "";
    const token = auth.startsWith("Bearer ") ? auth.slice(7) : "";
    if (!token) return res.status(401).json({ error: "missing bearer" });

    const { payload } = await jwtVerify(token, jwks, { issuer, audience });
    (req as any).user = payload;
    next();
  } catch (e) {
    return res.status(401).json({ error: "invalid token" });
  }
}
