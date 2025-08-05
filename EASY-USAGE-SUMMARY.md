# 🚀 Test Utils - Now Super Easy to Use!

## 🎯 **3 Easy Ways to Use test-utils.sh**

### **1. Interactive Menu (Easiest)** 🖱️
```bash
./test-utils-wrapper.sh
```
- Opens a menu with options
- Interactive API testing
- Health check interface
- Function documentation

### **2. One-Liner Commands** ⚡
```bash
./test-utils-easy.sh print_success "Hello World!"
./test-utils-easy.sh test_api_endpoint GET http://httpbin.org/status/200 200
./test-utils-easy.sh check_service_health "My Service" http://localhost:8080/health
```

### **3. Demo Script** 📺
```bash
./test-utils-demo.sh
```
- Shows all functions in action
- Complete demonstration
- Learning tool

## 📋 **Quick Examples**

### **Test CornerShop Services**
```bash
# Check if services are running
./test-utils-easy.sh check_service_health "API Gateway" "http://api.cornershop.localhost/health"

# Test API endpoints
./test-utils-easy.sh test_api_endpoint GET "http://api.cornershop.localhost/api/products" 200
./test-utils-easy.sh test_api_endpoint_with_key GET "http://api.cornershop.localhost/api/orders" "cornershop-api-key-2024" 200
```

### **Format Output**
```bash
./test-utils-easy.sh print_section "My Test Section"
./test-utils-easy.sh print_success "Test passed!"
./test-utils-easy.sh print_error "Test failed!"
./test-utils-easy.sh print_info "Processing..."
./test-utils-easy.sh print_warning "Slow response"
```

## 🎨 **What You'll See**

```
--- My Test Section ---
✅ Test passed!
❌ Test failed!
ℹ️  Processing...
⚠️  Slow response
```

## 📚 **Files Created**

| File | Purpose | Usage |
|------|---------|-------|
| `test-utils-wrapper.sh` | Interactive menu | `./test-utils-wrapper.sh` |
| `test-utils-easy.sh` | One-liner commands | `./test-utils-easy.sh function args` |
| `test-utils-demo.sh` | Complete demo | `./test-utils-demo.sh` |
| `TEST-UTILS-README.md` | Detailed guide | Read for full documentation |

## 🚀 **Get Started Now**

1. **Try the interactive menu:**
   ```bash
   ./test-utils-wrapper.sh
   ```

2. **Try a one-liner:**
   ```bash
   ./test-utils-easy.sh print_success "It works!"
   ```

3. **Run the demo:**
   ```bash
   ./test-utils-demo.sh
   ```

## ✅ **Benefits Achieved**

- ✅ **No more confusion** about how to use test-utils.sh
- ✅ **Multiple usage options** for different preferences
- ✅ **Interactive interface** for beginners
- ✅ **One-liner commands** for quick testing
- ✅ **Complete documentation** and examples
- ✅ **Real-world examples** for CornerShop services

---

**🎉 test-utils.sh is now super easy to use!** 