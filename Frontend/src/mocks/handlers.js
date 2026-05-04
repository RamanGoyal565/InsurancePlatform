import { http, HttpResponse } from 'msw';

const BASE = 'http://localhost:5000';

// Mock data
const mockUser = { userId: '1', name: 'Admin User', email: 'admin@example.com', role: 'Admin', isActive: true, createdAt: new Date().toISOString() };
const mockCustomer = { userId: '2', name: 'John Doe', email: 'john@example.com', role: 'Customer', isActive: true, createdAt: new Date().toISOString() };

const mockPolicies = [
  { policyId: 'p1', name: 'Comprehensive Car Insurance', vehicleType: 1, premium: 6999, coverageDetails: 'Own Damage,Third Party Liability,Passenger Cover,Roadside Assistance', terms: 'Annual policy', policyDocument: '' },
  { policyId: 'p2', name: 'Commercial Truck Policy', vehicleType: 2, premium: 14999, coverageDetails: 'Own Damage,Third Party Liability,Driver Cover,Roadside Assistance', terms: 'Annual policy', policyDocument: '' },
  { policyId: 'p3', name: 'Bike Insurance', vehicleType: 3, premium: 2299, coverageDetails: 'Own Damage,Third Party Liability,Personal Accident,Roadside Assistance', terms: 'Annual policy', policyDocument: '' },
];

const mockCustomerPolicies = [
  { customerPolicyId: 'cp1', policyId: 'p1', policyName: 'Comprehensive Car Insurance', vehicleType: 1, premium: 6999, vehicleNumber: 'DL01AB1234', drivingLicenseNumber: 'DL1420150001234', status: 2, startDate: '2025-08-11', endDate: '2026-08-15', lastPaymentFailureReason: null, lastPaymentFailedOnUtc: null },
  { customerPolicyId: 'cp2', policyId: 'p3', policyName: 'Bike Insurance', vehicleType: 3, premium: 2299, vehicleNumber: 'DL1LAE4567', drivingLicenseNumber: 'DL1420150005678', status: 1, startDate: '2025-05-22', endDate: '2026-05-22', lastPaymentFailureReason: null, lastPaymentFailedOnUtc: null },
];

const mockTickets = [
  { ticketId: 't1', title: 'Policy document not received', description: 'I purchased a policy but did not receive the document.', type: 1, status: 3, customerId: '2', assignedTo: '3', policyId: 'p1', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(), claimDetails: null },
  { ticketId: 't2', title: 'Claim for accident damage', description: 'My car was hit by another vehicle.', type: 2, status: 2, customerId: '2', assignedTo: '3', policyId: 'p1', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(), claimDetails: { claimId: 'cl1', ticketId: 't2', claimAmount: 48100, documents: '', approvalStatus: 2 } },
];

const mockPayments = [
  { paymentId: 'pay1', customerId: '2', policyId: 'p1', customerPolicyId: 'cp1', paymentReference: 'PAY-2026-000512', source: 'Manual', amount: 12499, status: 2, createdAt: new Date().toISOString() },
  { paymentId: 'pay2', customerId: '2', policyId: 'p3', customerPolicyId: 'cp2', paymentReference: 'PAY-2026-000511', source: 'PolicyWorkflow', amount: 4499, status: 3, createdAt: new Date().toISOString() },
];

const mockNotifications = [
  { notificationId: 'n1', userId: '2', message: 'Your Car Policy (POL-2026-8765) has been purchased successfully.', isRead: false, createdAt: new Date().toISOString() },
  { notificationId: 'n2', userId: '2', message: 'Payment of ₹4,499 for Bike Policy (POL-2026-4432) has failed.', isRead: false, createdAt: new Date().toISOString() },
  { notificationId: 'n3', userId: '2', message: 'Your Health Policy (POL-3221) has been renewed successfully.', isRead: true, createdAt: new Date().toISOString() },
];

const mockDashboard = { totalTickets: 158, openTickets: 42, totalClaims: 2356, approvedClaims: 1843, totalRevenue: 48200000, activePolicies: 15618, totalUsers: 12458 };

export const handlers = [
  // Auth
  http.post(`${BASE}/identity/auth/login`, async ({ request }) => {
    const body = await request.json();
    if (body.email === 'admin@example.com') {
      return HttpResponse.json({ accessToken: 'mock-admin-token', expiresAtUtc: new Date(Date.now() + 7200000).toISOString(), user: mockUser });
    }
    if (body.email === 'john@example.com') {
      return HttpResponse.json({ accessToken: 'mock-customer-token', expiresAtUtc: new Date(Date.now() + 7200000).toISOString(), user: mockCustomer });
    }
    return HttpResponse.json({ message: 'Invalid credentials' }, { status: 401 });
  }),

  http.post(`${BASE}/identity/auth/register`, async ({ request }) => {
    const body = await request.json();
    const newUser = { userId: crypto.randomUUID(), name: body.name, email: body.email, role: 'Customer', isActive: true, createdAt: new Date().toISOString() };
    return HttpResponse.json({ accessToken: 'mock-new-token', expiresAtUtc: new Date(Date.now() + 7200000).toISOString(), user: newUser });
  }),

  http.post(`${BASE}/identity/auth/logout`, () => new HttpResponse(null, { status: 204 })),

  // Users (Admin)
  http.get(`${BASE}/identity/admin/users`, () => HttpResponse.json([mockUser, mockCustomer,
    { userId: '3', name: 'Rohit Gupta', email: 'rohit@example.com', role: 'SupportSpecialist', isActive: true, createdAt: new Date().toISOString() },
    { userId: '4', name: 'Sandeep Kumar', email: 'sandeep@example.com', role: 'ClaimsSpecialist', isActive: true, createdAt: new Date().toISOString() },
  ])),

  http.post(`${BASE}/identity/admin/create-claims-specialist`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ userId: crypto.randomUUID(), ...body, role: 'ClaimsSpecialist', isActive: true, createdAt: new Date().toISOString() });
  }),

  http.post(`${BASE}/identity/admin/create-support-specialist`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ userId: crypto.randomUUID(), ...body, role: 'SupportSpecialist', isActive: true, createdAt: new Date().toISOString() });
  }),

  http.patch(`${BASE}/identity/admin/users/:userId/status`, async ({ params, request }) => {
    const body = await request.json();
    return HttpResponse.json({ userId: params.userId, isActive: body.isActive });
  }),

  // Policies
  http.get(`${BASE}/policies`, () => HttpResponse.json(mockPolicies)),
  http.get(`${BASE}/customer-policies`, () => HttpResponse.json(mockCustomerPolicies)),
  http.post(`${BASE}/policies`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ policyId: crypto.randomUUID(), ...body });
  }),
  http.put(`${BASE}/policies/:policyId`, async ({ params, request }) => {
    const body = await request.json();
    return HttpResponse.json({ policyId: params.policyId, ...body });
  }),
  http.post(`${BASE}/purchase`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ customerPolicyId: crypto.randomUUID(), ...body, status: 1 });
  }),
  http.post(`${BASE}/customer-policies/:id/renew`, ({ params }) =>
    HttpResponse.json({ customerPolicyId: params.id, status: 1 })
  ),

  // Tickets
  http.get(`${BASE}/tickets`, () => HttpResponse.json(mockTickets)),
  http.post(`${BASE}/tickets`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ ticketId: crypto.randomUUID(), ...body, status: 1, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() });
  }),
  http.get(`${BASE}/tickets/:ticketId/comments`, () => HttpResponse.json([])),
  http.post(`${BASE}/tickets/:ticketId/comments`, async ({ params, request }) => {
    const body = await request.json();
    return HttpResponse.json({ commentId: crypto.randomUUID(), ticketId: params.ticketId, userId: '2', message: body.message, createdAt: new Date().toISOString() });
  }),
  http.put(`${BASE}/tickets/:ticketId/status`, async ({ params, request }) => {
    const body = await request.json();
    return HttpResponse.json({ ticketId: params.ticketId, status: body.status });
  }),
  http.put(`${BASE}/tickets/:ticketId/assign`, async ({ params, request }) => {
    const body = await request.json();
    return HttpResponse.json({ ticketId: params.ticketId, assignedTo: body.assignedToUserId });
  }),
  http.post(`${BASE}/tickets/:ticketId/approve`, ({ params }) =>
    HttpResponse.json({ ticketId: params.ticketId, approved: true })
  ),
  http.post(`${BASE}/tickets/:ticketId/reject`, ({ params }) =>
    HttpResponse.json({ ticketId: params.ticketId, rejected: true })
  ),

  // Payments
  http.get(`${BASE}/payments`, () => HttpResponse.json(mockPayments)),
  http.post(`${BASE}/payments`, async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json({ paymentId: crypto.randomUUID(), ...body, status: 2, createdAt: new Date().toISOString() });
  }),

  // Notifications
  http.get(`${BASE}/notifications`, () => HttpResponse.json(mockNotifications)),
  http.post(`${BASE}/notifications/:notificationId/read`, ({ params }) =>
    HttpResponse.json({ notificationId: params.notificationId, isRead: true })
  ),

  // Admin reports
  http.get(`${BASE}/admin/dashboard`, () => HttpResponse.json(mockDashboard)),
  http.get(`${BASE}/admin/reports/tickets`, () => HttpResponse.json({ totalTickets: 158, ticketsByStatus: [{ label: 'Open', count: 42 }, { label: 'Assigned', count: 36 }, { label: 'InReview', count: 28 }, { label: 'Resolved', count: 38 }, { label: 'Closed', count: 14 }], ticketsByType: [{ label: 'Support', count: 100 }, { label: 'Claim', count: 58 }], openTickets: 42, closedTickets: 14, averageResolutionTimeHours: 36 })),
  http.get(`${BASE}/admin/reports/claims`, () => HttpResponse.json({ totalClaims: 2356, claimsByStatus: [], claimApprovalRate: 78.2, claimRejectionRate: 21.8, averageClaimProcessingTimeHours: 48 })),
  http.get(`${BASE}/admin/reports/revenue`, () => HttpResponse.json({ totalRevenue: 48200000, revenueByDate: { daily: [{ period: 'Mar 27', amount: 125000 }, { period: 'Mar 28', amount: 98000 }, { period: 'Mar 29', amount: 145000 }, { period: 'Mar 30', amount: 110000 }, { period: 'Apr 01', amount: 160000 }, { period: 'Apr 02', amount: 130000 }, { period: 'Apr 03', amount: 175000 }], monthly: [], yearly: [] }, revenueByPolicyType: [], revenuePerCustomer: [], revenueTrends: [{ period: 'Jan', amount: 3200000 }, { period: 'Feb', amount: 4100000 }, { period: 'Mar', amount: 4800000 }] })),
  http.get(`${BASE}/admin/reports/policies`, () => HttpResponse.json({ totalPoliciesSold: 18742, activePolicies: 15618, expiredPolicies: 1200, policiesByType: [{ label: 'Car', count: 10256 }, { label: 'Truck', count: 4632 }, { label: 'Bike', count: 3854 }], policyRenewalRate: 72.5 })),
  http.get(`${BASE}/admin/reports/users`, () => HttpResponse.json({ totalUsers: 12458, usersByRole: { customers: 9352, claimsSpecialists: 748, supportSpecialists: 1842 }, activeUsers: 11200, inactiveUsers: 1258 })),
  http.get(`${BASE}/admin/reports/performance`, () => HttpResponse.json({ claimsSpecialists: [{ userId: 'u1', name: 'Arjun Singh', claimsProcessed: 128, approvalRate: 75, averageProcessingTimeHours: 43.2 }, { userId: 'u2', name: 'Pooja Kapoor', claimsProcessed: 115, approvalRate: 77.4, averageProcessingTimeHours: 50.4 }], supportSpecialists: [] })),
  http.get(`${BASE}/admin/reports/policies/:policyId/customers`, () => HttpResponse.json({ policyId: 'p1', policyName: 'Comprehensive Car Insurance', vehicleType: 'Car', totalCustomers: 245, customers: [{ customerId: '2', customerName: 'Rahul Sharma', customerEmail: 'rahul.sharma@email.com', isActive: true, firstPurchasedAtUtc: '2026-02-28', policiesBought: 1, renewalCount: 1, latestStatus: 'Active' }] })),
];
