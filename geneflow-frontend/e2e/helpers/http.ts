/**
 * Permission-test status assertions.
 * Backends differ on 401 vs 403 vs 404 when authorization is lacking,
 * so these helpers normalize the expected response classes.
 */
export const isClientError = (s: number) => s >= 400 && s < 500;
export const isForbidden = (s: number) => s === 401 || s === 403 || s === 404;
export const isUnauthorized = (s: number) => s === 401 || s === 403;
export const isSuccess = (s: number) => s >= 200 && s < 300;
