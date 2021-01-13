using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.ModelBinding
{
    public class ModelBindingController : ControllerBase
    {
        private static readonly IActionResult _emptyResult = new EmptyResult();
        private static readonly IActionResult _badRequestResult = new BadRequestResult();

        // We want to track multiple modelbinding scenarios:
        // The work we do to bind the "simplest" of values (a string).
        // The work we do perform simple value conversions (string -> int)
        // If there is a performance difference between being explicit ([FromRoute/Query]) over the binding source being implicit.
        // The impact of binding a single parameter vs binding lots of parameters in the overal context of the request.
        // The work we do to bind "DTOs" from the query string, the route and the body
        // The work we do to bind "DTOs" with nested objects (acyclic graphs) from the query string, the route and the body

        // No modelbinding involved
        [HttpGet("/Modelbinding/None")]
        public IActionResult NoModelBinding() => _emptyResult;

        // Simple types binding

        [HttpGet("/Modelbinding/FromRoute/String/{value}")]
        public IActionResult RouteBindString([FromRoute] string value) => _emptyResult;

        [HttpGet("/Modelbinding/FromRoute/Int32/{value}")]
        public IActionResult RouteBindInteger([FromRoute] int value) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/Implicit/FromRoute/String/{value}")]
        public IActionResult RouteBindStringImplicitly(string value) => _emptyResult;

        [HttpGet("/Modelbinding/FromRoute/String_String_String/{value1}/{value2}/{value3}")]
        public IActionResult RouteBindMultipleString([FromRoute] string value1, [FromRoute] string value2, [FromRoute] string value3) => _emptyResult;

        [HttpGet("/Modelbinding/FromRoute/Int32_Int32_Int32/{value1}/{value2}/{value3}")]
        public IActionResult RouteBindMultipleInt([FromRoute] int value1, [FromRoute] int value2, [FromRoute] int value3) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/FromRoute/String/")]
        public IActionResult QueryBindString([FromQuery] string value) => _emptyResult;

        [HttpGet("/Modelbinding/FromQuery/Int32/")]
        public IActionResult QueryBindInteger([FromQuery] int value) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/Implicit/FromQuery/String/")]
        public IActionResult QueryBindStringImplicitly(string value) => _emptyResult;

        [HttpGet("/Modelbinding/FromQuery/String_String_String/")]
        public IActionResult QueryBindMultipleString([FromQuery] string value1, [FromQuery] string value2, [FromQuery] string value3) => _emptyResult;

        [HttpGet("/Modelbinding/FromQuery/Int32_Int32_Int32/")]
        public IActionResult QueryBindMultipleInt([FromQuery] int value1, [FromQuery] int value2, [FromQuery] int value3) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        // DTOs
        [HttpPost("/Modelbinding/BasicDTO/FromBody")]
        public IActionResult DtoFromBody([FromBody] BasicDto dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/BasicDTO/FromQuery")]
        public IActionResult DtoFromQuery([FromQuery] BasicDto dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpPost("/Modelbinding/BasicDTO/FromForm")]
        public IActionResult DtoFromForm([FromForm] BasicDto dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        // DTOs with nested objects
        [HttpPost("/Modelbinding/ComplexDto/FromBody")]
        public IActionResult ComplexDtoFromBody([FromBody] ComplexDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/ComplexDto/FromQuery")]
        public IActionResult ComplexDtoFromQuery([FromQuery] ComplexDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpPost("/Modelbinding/ComplexDto/FromForm")]
        public IActionResult ComplexDtoFromForm([FromForm] ComplexDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        // DTOs with nested objects and validation
        [HttpPost("/Modelbinding/ComplexDtoWithValidation/FromBody")]
        public IActionResult ComplexDtoWithValidationFromBody([FromBody] ComplexValidationDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpGet("/Modelbinding/ComplexDtoWithValidation/FromQuery")]
        public IActionResult ComplexDtoWithValidationFromQuery([FromQuery] ComplexValidationDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;

        [HttpPost("/Modelbinding/ComplexDtoWithValidation/FromForm")]
        public IActionResult ComplexDtoWithValidationFromForm([FromForm] ComplexValidationDtoRoot dto) => ModelState.IsValid ? _emptyResult : _badRequestResult;
    }

    public class BasicDto
    {
        public string Street { get; set; }
        public int StreetNumber { get; set; }

        public string ResidenceType { get; set; }
        public string ResidenceNumber { get; set; }

        public string Country { get; set; }
        public string ZipCode { get; set; }
    }

    // Customer
    public class ComplexDtoRoot
    {
        public string Id { get; set; }
        public string Company { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Number { get; set; }
        public string Fax { get; set; }

        public IList<ComplexDtoIntermediateNode> Orders { get; set; }
    }

    //Order
    public class ComplexDtoIntermediateNode
    {
        public int Id { get; set; }
        public string ComplexDtoRootId { get; set; }
        public DateTimeOffset OrderDate { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public DateTimeOffset ShipDate { get; set; }
        public int Via { get; set; }
        public decimal Freight { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipCountry { get; set; }

        public IList<ComplexDtoLeafNode> OrderDetails { get; set; }
    }

    // Order details
    public class ComplexDtoLeafNode
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public short Quantity { get; set; }
        public double Discount { get; set; }
    }

    // Customer with validation
    public class ComplexValidationDtoRoot
    {
        [Required]
        [StringLength(5)]
        public string Id { get; set; }

        [StringLength(40)]
        public string Company { get; set; }

        [StringLength(30)]
        public string Name { get; set; }

        [StringLength(30)]
        public string Title { get; set; }

        [StringLength(60)]
        public string Address { get; set; }

        [StringLength(15)]
        public string City { get; set; }

        [StringLength(15)]
        public string Region { get; set; }

        [StringLength(10)]
        public string PostalCode { get; set; }

        [StringLength(15)]
        public string Country { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Number { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Fax { get; set; }

        public IList<ComplexValidationDtoIntermediateNode> Orders { get; set; }
    }

    //Order
    public class ComplexValidationDtoIntermediateNode
    {
        [Required]
        public int Id { get; set; }

        // Leave the ID as it is even though it doesn't match the
        // name in the parent entity so that we can reuse the same JSON body
        [Required]
        [StringLength(5)]
        public string ComplexDtoRootId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTimeOffset OrderDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTimeOffset DueDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTimeOffset? ShipDate { get; set; }

        public int ShipCompany { get; set; }

        [Required]
        public decimal Freight { get; set; }

        [StringLength(60)]
        public string ShipAddress { get; set; }

        [StringLength(15)]
        public string ShipCity { get; set; }

        [StringLength(15)]
        public string ShipRegion { get; set; }

        [StringLength(10)]
        public string ShipPostalCode { get; set; }

        [StringLength(15)]
        public string ShipCountry { get; set; }

        public IList<ComplexValidationDtoLeafNode> OrderDetails { get; set; }
    }

    // Order details
    public class ComplexValidationDtoLeafNode
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public short Quantity { get; set; }
        public double Discount { get; set; }
    }
}
