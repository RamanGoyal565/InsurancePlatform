/** Exact role strings as returned by the backend */
export const ROLES = {
  ADMIN: 'Admin',
  CUSTOMER: 'Customer',
  CLAIMS_SPECIALIST: 'ClaimsSpecialist',
  SUPPORT_SPECIALIST: 'SupportSpecialist',
};

/**
 * Returns true if the user has at least one of the required roles.
 * @param {import('./AuthContext').CurrentUser|null} user
 * @param {string[]} roles
 */
export function hasRole(user, roles) {
  if (!user) return false;
  return roles.includes(user.role);
}
