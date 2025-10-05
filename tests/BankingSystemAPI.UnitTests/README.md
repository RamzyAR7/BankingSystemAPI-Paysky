// README for Test Organization
// 
// This test project follows a structured approach to organize tests by layer and functionality:
//
// Structure:
// ??? UnitTests/                           # All unit tests organized by source project structure
// ?   ??? Domain/                          # Domain layer tests
// ?   ?   ??? Entities/                    # Entity business logic tests
// ?   ?   ??? Common/                      # Common domain components (Result pattern, etc.)
// ?   ??? Application/                     # Application layer tests  
// ?   ?   ??? Features/                    # Feature-based command/query handler tests
// ?   ?   ?   ??? Identity/                # Identity management features
// ?   ?   ?   ??? Transactions/            # Transaction features
// ?   ?   ?   ??? Accounts/                # Account features
// ?   ?   ?   ??? Banks/                   # Bank features
// ?   ?   ??? Authorization/               # Authorization service tests
// ?   ?   ??? DTOs/                        # Data Transfer Object tests
// ?   ??? Infrastructure/                  # Infrastructure layer tests
// ?   ?   ??? Identity/                    # Identity service implementations
// ?   ?   ??? Repositories/                # Repository implementations
// ?   ?   ??? Services/                    # Other infrastructure services
// ?   ??? Presentation/                    # Presentation layer tests (if needed)
// ?       ??? Controllers/                 # Controller tests
// ??? IntegrationTests/                    # Integration tests (future)
// ??? TestInfrastructure/                  # Test utilities and helpers
// ??? GlobalUsings.cs                      # Global using directives
//
// Naming Conventions:
// - Test classes: [ClassName]Tests.cs
// - Test methods: [MethodName]_[Scenario]_[ExpectedResult]
// - Use descriptive test names that explain the business scenario
//
// Test Categories:
// - Unit Tests: Test individual components in isolation
// - Integration Tests: Test component interactions
// - Business Logic Tests: Focus on domain business rules
// - Authorization Tests: Test access control and permissions
// - Validation Tests: Test input validation and business rules
//
// Best Practices:
// - Each test class inherits from TestBase for common setup
// - Use TestEntityFactory for creating test entities
// - Use TestDtoBuilder for creating test DTOs
// - Mock external dependencies
// - Test both happy path and error scenarios
// - Use Theory tests for parameterized testing