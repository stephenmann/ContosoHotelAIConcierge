using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ContosoHotels.Migrations
{
    public partial class AddAIConciergeModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationHistories",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(nullable: false),
                    SessionId = table.Column<string>(maxLength: 100, nullable: false),
                    UserId = table.Column<string>(maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    IpAddress = table.Column<string>(maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationHistories", x => x.ConversationId);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    MenuItemId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    Category = table.Column<string>(maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsAvailable = table.Column<bool>(nullable: false),
                    DietaryInfo = table.Column<string>(maxLength: 500, nullable: true),
                    PreparationTimeMinutes = table.Column<int>(nullable: false),
                    ImageUrl = table.Column<string>(maxLength: 255, nullable: true),
                    PopularityScore = table.Column<int>(nullable: false),
                    IsCustomizable = table.Column<bool>(nullable: false),
                    SpiceLevel = table.Column<int>(nullable: false),
                    Available24Hours = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.MenuItemId);
                });

            migrationBuilder.CreateTable(
                name: "AgentInteractions",
                columns: table => new
                {
                    InteractionId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<Guid>(nullable: false),
                    AgentType = table.Column<string>(maxLength: 50, nullable: false),
                    Action = table.Column<string>(maxLength: 100, nullable: false),
                    Success = table.Column<bool>(nullable: false),
                    Duration = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    ErrorMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    ActionContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntentConfidence = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentInteractions", x => x.InteractionId);
                    table.ForeignKey(
                        name: "FK_AgentInteractions_ConversationHistories_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ConversationHistories",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    MessageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<Guid>(nullable: false),
                    IsFromUser = table.Column<bool>(nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentType = table.Column<string>(maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    MessageMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<int>(nullable: false),
                    ContainsSensitiveData = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ConversationHistories_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ConversationHistories",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentInteractions_AgentType",
                table: "AgentInteractions",
                column: "AgentType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentInteractions_ConversationId",
                table: "AgentInteractions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentInteractions_Timestamp",
                table: "AgentInteractions",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Timestamp",
                table: "ChatMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_SessionId",
                table: "ConversationHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_StartTime",
                table: "ConversationHistories",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_UserId",
                table: "ConversationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Category",
                table: "MenuItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_IsAvailable",
                table: "MenuItems",
                column: "IsAvailable");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentInteractions");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "ConversationHistories");
        }
    }
}
